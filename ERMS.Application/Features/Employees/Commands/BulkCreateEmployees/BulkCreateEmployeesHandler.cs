using ERMS.Application.Interface;
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
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Commands.BulkCreateEmployees
{
    public sealed class BulkCreateEmployeesHandler : IRequestHandler<BulkCreateEmployeesCommand, BulkCreateResult>
    {
        private readonly IERMSDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<BulkCreateEmployeesHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;

        public BulkCreateEmployeesHandler(
            IERMSDbContext context,
            UserManager<User> userManager,
            ILogger<BulkCreateEmployeesHandler> logger,
            ICurrentUserService currentUserService,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _currentUserService = currentUserService;
            _emailService = emailService;
        }

        public async Task<BulkCreateResult> Handle(BulkCreateEmployeesCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) throw new UnauthorizedAccessException("User not belong to enterprise");

            var result = new BulkCreateResult
            {
                TotalCount = request.Items.Count
            };

            // Get enterprise
            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, cancellationToken);

            if (enterprise == null)
            {
                throw new Exception("Doanh nghiệp không tồn tại");
            }

            // Get all departments for this enterprise
            var departments = await _context.Departments
                .Where(d => d.EnterpriseId == enterpriseId && !d.IsDeleted)
                .ToDictionaryAsync(d => d.DepartmentCode ?? "", cancellationToken);

            // Get current employee count for code generation
            var employeeCount = await _context.Employees
                .CountAsync(e => e.EnterpriseId == enterpriseId, cancellationToken);

            // Track successfully created employees for email sending
            var createdEmployees = new List<(string Email, string FullName, string Password)>();

            for (int i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                try
                {
                    // Validate department
                    if (!departments.TryGetValue(item.DepartmentCode, out var department))
                    {
                        result.Errors.Add(new BulkCreateError
                        {
                            RowIndex = i + 1,
                            Email = item.Email,
                            ErrorMessage = $"Phòng ban '{item.DepartmentCode}' không tồn tại"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    // Check email exists
                    var existingUser = await _userManager.FindByEmailAsync(item.Email);
                    if (existingUser != null)
                    {
                        result.Errors.Add(new BulkCreateError
                        {
                            RowIndex = i + 1,
                            Email = item.Email,
                            ErrorMessage = "Email đã được sử dụng"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    // Use provided password or generate random one
                    var password = string.IsNullOrWhiteSpace(item.Password) 
                        ? GenerateRandomPassword() 
                        : item.Password;

                    // Create User
                    var user = new User
                    {
                        Id = Guid.CreateVersion7(),
                        UserName = item.Email,
                        Email = item.Email,
                        FullName = item.FullName,
                        PhoneNumber = item.Phone,
                        EmailConfirmed = true,
                        DateJoined = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user, password);
                    if (!createResult.Succeeded)
                    {
                        result.Errors.Add(new BulkCreateError
                        {
                            RowIndex = i + 1,
                            Email = item.Email,
                            ErrorMessage = string.Join(", ", createResult.Errors.Select(e => e.Description))
                        });
                        result.FailedCount++;
                        continue;
                    }

                    // Determine role: use provided role or default to Employee
                    var validRoles = new[] { "Employee", "Trainer", "Director", "DepartmentHead" };
                    var roleName = !string.IsNullOrWhiteSpace(item.Role) && validRoles.Contains(item.Role, StringComparer.OrdinalIgnoreCase)
                        ? validRoles.First(r => r.Equals(item.Role, StringComparison.OrdinalIgnoreCase))
                        : "Employee";
                    
                    await _userManager.AddToRoleAsync(user, roleName);

                    // Create Employee
                    employeeCount++;
                    var employee = new Employee
                    {
                        Id = Guid.CreateVersion7(),
                        UserId = user.Id,
                        EnterpriseId = enterpriseId.Value,
                        DepartmentId = department.Id,
                        EmployeeCode = $"{enterprise.EnterpriseCode}-{employeeCount:D4}",
                        Position = item.Position,
                        EmploymentType = "FullTime",
                        HireDate = DateTime.UtcNow,
                        Status = "Active",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Employees.Add(employee);
                    result.SuccessCount++;

                    // Track for email sending
                    createdEmployees.Add((item.Email, item.FullName, password));
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new BulkCreateError
                    {
                        RowIndex = i + 1,
                        Email = item.Email,
                        ErrorMessage = ex.Message
                    });
                    result.FailedCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Send welcome emails to created employees (fire and forget, don't block)
            _ = Task.Run(async () =>
            {
                foreach (var emp in createdEmployees)
                {
                    try
                    {
                        await SendWelcomeEmailAsync(emp.Email, emp.FullName, emp.Password, enterprise.EnterpriseName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send welcome email to {Email}", emp.Email);
                    }
                }
            });

            _logger.LogInformation("Bulk created {SuccessCount}/{TotalCount} employees for enterprise {EnterpriseId}",
                result.SuccessCount, result.TotalCount, enterpriseId);

            return result;
        }

        /// <summary>
        /// Generate a random password that meets ASP.NET Identity requirements:
        /// - At least 8 characters
        /// - At least one uppercase letter
        /// - At least one lowercase letter
        /// - At least one digit
        /// - At least one special character
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

            // Shuffle the password to avoid predictable pattern
            return new string(password.ToString().ToCharArray().OrderBy(_ => RandomNumberGenerator.GetInt32(100)).ToArray());
        }
        // đoạn này t hơi bí nên đặt ra 1 commands riêng không hay để luôn ở đây, tại đúng ra thì nó nằm trong luồng tạo nhân viên luôn chứ không độc lập, m thấy sao ?
        // t ko thấy vấn đề gì khi để nó ở đây cả, vì nó chỉ phục vụ cho việc gửi email sau khi tạo nhân viên thành công mà
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
