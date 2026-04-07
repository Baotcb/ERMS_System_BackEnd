using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Commands.ImportEmployeesFromFile
{
    public sealed class ImportEmployeesFromFileHandler : IRequestHandler<ImportEmployeesFromFileCommand, ImportEmployeesFromFileResult>
    {
        private readonly IERMSDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ImportEmployeesFromFileHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly IExcelParserService _excelParser;

        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public ImportEmployeesFromFileHandler(
            IERMSDbContext context,
            UserManager<User> userManager,
            ILogger<ImportEmployeesFromFileHandler> logger,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IExcelParserService excelParser)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _excelParser = excelParser;
        }

        public async Task<ImportEmployeesFromFileResult> Handle(ImportEmployeesFromFileCommand request, CancellationToken cancellationToken)
        {
            var result = new ImportEmployeesFromFileResult();

            try
            {
                // 1. Parse file
                var parseResult = _excelParser.ParseEmployeeImportFile(
                    request.File.OpenReadStream(),
                    request.File.FileName
                );

                // 2. Copy parse info to result
                result.ColumnMappings = parseResult.ColumnMappings
                    .Select(cm => new ColumnMappingResult
                    {
                        OriginalHeader = cm.OriginalHeader,
                        MappedKey = cm.MappedKey
                    }).ToList();

                result.UnknownColumns = parseResult.UnknownColumns.ToList();

                result.Warnings = parseResult.Warnings
                    .Select(w => new ParseWarningResult
                    {
                        Type = w.Type,
                        Column = w.Column,
                        Message = w.Message
                    }).ToList();

                result.TotalRows = parseResult.Rows.Count;

                // 3. Nếu có lỗi parse structure, return ngay
                if (!parseResult.IsValid)
                {
                    result.Errors = parseResult.Errors
                        .Select(e => new ImportError
                        {
                            RowNumber = e.RowNumber,
                            Email = null,
                            Column = e.Column,
                            Message = e.Message
                        }).ToList();

                    return result;
                }

                // 4. Validate Environment (Enterprise, Role)
                var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
                if (enterpriseId == null) throw new UnauthorizedAccessException("Không tìm thấy thông tin doanh nghiệp.");
                
                var userRoles = _currentUserService.Roles;
                if (userRoles == null || !userRoles.Contains("HRManager")) throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền import.");

                var enterprise = await _context.Enterprises
                    .FirstOrDefaultAsync(e => e.Id == enterpriseId.Value && !e.IsDeleted, cancellationToken);
                if (enterprise == null) throw new Exception("Doanh nghiệp không tồn tại");

                // 5. Pre-load Data for Validation
                var departments = await _context.Departments
                    .Where(d => d.EnterpriseId == enterpriseId.Value && !d.IsDeleted)
                    .ToDictionaryAsync(d => (d.DepartmentCode ?? "").ToUpperInvariant(), cancellationToken);

                var employeeCount = await _context.Employees
                    .CountAsync(e => e.EnterpriseId == enterpriseId, cancellationToken);

                // 6. Process Rows (Validation & Execution)
                var validRows = parseResult.Rows.Where(r => r.IsValid).ToList();
                result.ValidRows = validRows.Count;
                var createdEmployees = new List<(string Email, string FullName, string Password)>();

                // Nếu là Commit, check xem có lỗi nào không trước khi chạy (nếu mode là 'Strict')
                // Hiện tại logic: DryRun trả về Errors. Commit sẽ cố chạy, skip row lỗi. 
                // Tuy nhiên, User yêu cầu "Validate chuẩn chỉ... nếu chưa thì không thêm".
                // Nghĩa là: nếu có bất kỳ lỗi logic nào (vd Email trùng), thì Commit cũng nên Fail toàn bộ?
                // Để an toàn và đáp ứng user: Trong Validate Phase (DryRun), ta check logic.
                // Trong Commit Phase, ta cũng check logic. 
                // Nếu Commit=true, ta dùng Transaction từng row hoặc toàn bộ. User muốn "Create User/Emp" atomic.
                
                // Logic check distinct email trong file excel
                var emailInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in validRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.Email))
                    {
                        if (emailInFile.Contains(row.Email))
                        {
                             result.Errors.Add(new ImportError { RowNumber = row.RowNumber, Email = row.Email, Column = "Email", Message = "Email bị trùng lặp trong file Excel" });
                             result.FailedCount++;
                        }
                        else
                        {
                            emailInFile.Add(row.Email);
                        }
                    }
                }

                // Nếu DryRun, check logic DB
                if (!request.Commit)
                {
                    foreach (var row in validRows)
                    {
                        var role = row.Role ?? AppRoles.Employee;
                        var isDirectorRole = role.Equals(AppRoles.Director, StringComparison.OrdinalIgnoreCase);
                        // Check Dept
                        if (!isDirectorRole && !departments.ContainsKey(row.DepartmentCode?.ToUpperInvariant() ?? ""))
                        {
                             result.Errors.Add(new ImportError { RowNumber = row.RowNumber, Email = row.Email, Column = "DepartmentCode", Message = $"Phòng ban '{row.DepartmentCode}' không tồn tại" });
                             result.FailedCount++;
                             // Don't count as success in validation if failed here
                             // (Note: result.ValidRows is just "Parsable Rows", result.SuccessCount will be "Actionable Rows")
                             continue; 
                        }

                        // Check User Exists
                        var userExists = await _userManager.FindByEmailAsync(row.Email);
                        if (userExists != null)
                        {
                             result.Errors.Add(new ImportError { RowNumber = row.RowNumber, Email = row.Email, Column = "Email", Message = "Email đã tồn tại trong hệ thống" });
                             result.FailedCount++;
                             continue;
                        }

                        result.SuccessCount++; 
                    }
                    
                    // Return Validation Report
                    return result;
                }

                // EXECUTION PHASE (Commit = true)
                // Transactional Process per Row to prevent Zombie Users
                
                foreach (var row in validRows)
                {
                    var roleToAdd = row.Role ?? AppRoles.Employee;
                    var isDirectorRole = roleToAdd.Equals(AppRoles.Director, StringComparison.OrdinalIgnoreCase);

                    // Re-validate critical constraints to be sure
                    Department? department = null;
                    if (!isDirectorRole && !departments.TryGetValue(row.DepartmentCode?.ToUpperInvariant() ?? "", out department))
                    {
                        result.Errors.Add(new ImportError { RowNumber = row.RowNumber, Email = row.Email, Column = "DepartmentCode", Message = $"Phòng ban '{row.DepartmentCode}' không tồn tại" });
                        result.FailedCount++;
                        continue;
                    }

                    var existingUser = await _userManager.FindByEmailAsync(row.Email ?? "");
                    if (existingUser != null)
                    {
                        result.Errors.Add(new ImportError { RowNumber = row.RowNumber, Email = row.Email, Column = "Email", Message = "Email đã được sử dụng" });
                        result.FailedCount++;
                        continue;
                    }

                    // Create User & Employee Atomically (Manual Compensation)
                    User user = null;
                    try
                    {
                        var password = string.IsNullOrWhiteSpace(row.Password) ? GenerateRandomPassword() : row.Password;
                        
                        user = new User
                        {
                            Id = Guid.CreateVersion7(),
                            UserName = row.Email,
                            Email = row.Email,
                            FullName = row.FullName,
                            PhoneNumber = row.Phone,
                            EmailConfirmed = true,
                            DateJoined = DateTime.UtcNow
                        };

                        var createResult = await _userManager.CreateAsync(user, password);
                        if (!createResult.Succeeded)
                        {
                            result.Errors.Add(new ImportError { RowNumber = row.RowNumber, Email = row.Email, Column = "User", Message = string.Join(", ", createResult.Errors.Select(e => e.Description)) });
                            result.FailedCount++;
                            continue;
                        }

                        // Role
                        var addRoleResult = await _userManager.AddToRoleAsync(user, roleToAdd);
                        if (!addRoleResult.Succeeded)
                        {
                            result.Errors.Add(new ImportError { RowNumber = row.RowNumber, Email = row.Email, Column = "Role", Message = string.Join(", ", addRoleResult.Errors.Select(e => e.Description)) });
                            result.FailedCount++;
                            await _userManager.DeleteAsync(user);
                            user = null;
                            continue;
                        }

                        // Employee
                        employeeCount++;
                        var employee = new Employee
                        {
                            Id = Guid.CreateVersion7(),
                            UserId = user.Id,
                            EnterpriseId = enterpriseId.Value,
                            DepartmentId = isDirectorRole ? null : department!.Id,
                            EmployeeCode = $"{enterprise.EnterpriseCode}-{employeeCount:D4}",
                            Position = row.Position,
                            EmploymentType = "FullTime",
                            HireDate = DateTime.UtcNow,
                            Status = "Active",
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Employees.Add(employee);
                        
                        // Save Employee IMMEDIATELY to ensure consistency
                        await _context.SaveChangesAsync(cancellationToken);

                        result.SuccessCount++;
                        createdEmployees.Add((row.Email ?? "", row.FullName ?? "", password));
                    }
                    catch (Exception ex)
                    {
                        // COMPENSATION: Delete User if Employee creation failed
                        if (user != null)
                        {
                            _logger.LogWarning("Đang hoàn tác người dùng {Email} do lỗi: {Error}", row.Email, ex.Message);
                            try { await _userManager.DeleteAsync(user); } catch (Exception delEx) { _logger.LogError(delEx, "Không thể hoàn tác người dùng {Email}", row.Email); }
                        }

                        result.Errors.Add(new ImportError { RowNumber = row.RowNumber, Email = row.Email, Column = "Import", Message = ex.Message });
                        result.FailedCount++;
                    }
                }

                // Send Emails
                if (createdEmployees.Any())
                {
                    _ = Task.Run(async () =>
                    {
                        foreach (var emp in createdEmployees)
                        {
                            try { await SendWelcomeEmailAsync(emp.Email, emp.FullName, emp.Password, enterprise.EnterpriseName); }
                            catch (Exception ex) { _logger.LogWarning(ex, "Gửi email thất bại đến {Email}", emp.Email); }
                        }
                    });
                }

                _logger.LogInformation("Đã nhập {SuccessCount}/{TotalRows} nhân viên.", result.SuccessCount, result.TotalRows);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nhập nhân viên");
                throw;
            }
        }

        /// <summary>
        /// Generate a random password that meets ASP.NET Identity requirements
        /// </summary>
        private static string GenerateRandomPassword(int length = 12)
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            const string allChars = uppercase + lowercase + digits + special;

            var password = new StringBuilder();

            // Ensure at least one of each required type
            password.Append(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
            password.Append(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
            password.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            password.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);

            // Fill remaining length with random characters
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
            }

            // Shuffle password to avoid predictable pattern
            return new string(password.ToString().ToCharArray().OrderBy(_ => RandomNumberGenerator.GetInt32(100)).ToArray());
        }

        /// <summary>
        /// Send welcome email to new employee
        /// </summary>
        private async Task SendWelcomeEmailAsync(string email, string fullName, string password, string enterpriseName)
        {
            var subject = $"Chào mừng bạn đến với {enterpriseName} - Thông tin tài khoản";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #0F4C75 0%, #1B6CA8 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .credentials {{ background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #0F4C75; margin: 20px 0; }}
        .credentials p {{ margin: 10px 0; }}
        .password {{ font-family: monospace; font-size: 18px; background: #e8f4fc; padding: 10px 15px; border-radius: 4px; display: inline-block; }}
        .warning {{ color: #e74c3c; font-size: 14px; margin-top: 20px; }}
        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chào mừng {fullName}!</h1>
            <p>Bạn đã được thêm vào hệ thống ERMS của {enterpriseName}</p>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{fullName}</strong>,</p>
            <p>Tài khoản của bạn đã được tạo thành công. Dưới đây là thông tin đăng nhập:</p>

            <div class='credentials'>
                <p><strong>📧 Email:</strong> {email}</p>
                <p><strong>🔑 Mật khẩu:</strong> <span class='password'>{password}</span></p>
            </div>

            <p class='warning'>⚠️ Vui lòng đổi mật khẩu ngay sau khi đăng nhập lần đầu để bảo mật tài khoản.</p>

            <p>Trân trọng,<br><strong>Đội ngũ {enterpriseName}</strong></p>
        </div>
        <div class='footer'>
            <p>Email này được gửi tự động từ hệ thống ERMS. Vui lòng không trả lời.</p>
        </div>
    </div>
</body>
</html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }
    }
}
