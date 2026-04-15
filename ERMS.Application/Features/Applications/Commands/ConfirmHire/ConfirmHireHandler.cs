using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ERMS.Application.Features.Applications.Commands.ConfirmHire;

/// <summary>
/// Handler for confirming a hire after the candidate signs the contract.
/// Creates a new User account (Employee role) and Employee profile.
/// Only HRManager can perform this action.
/// </summary>
public sealed class ConfirmHireHandler : IRequestHandler<ConfirmHireCommand, ConfirmHireResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<ConfirmHireHandler> _logger;

    public ConfirmHireHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<ConfirmHireHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ConfirmHireResult> Handle(ConfirmHireCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: HRManager ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có thể thực hiện hành động này.");
        }

        // 3. Enterprise scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 4. Load Application with related entities
        var application = await _context.Applications
            .Include(a => a.JobPosting)
            .Include(a => a.Offer)
            .Include(a => a.Candidate)
                .ThenInclude(c => c.User)
            .Include(a => a.ExternalCandidate)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, cancellationToken)
            ?? throw new Exception($"Không tìm thấy hồ sơ ứng tuyển với ID {request.ApplicationId}.");

        // 5. Validate enterprise ownership
        if (application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hồ sơ này.");
        }

        // 6. Validate offer exists and is accepted
        if (application.Offer == null || application.Offer.IsDeleted)
        {
            throw new Exception("Hồ sơ ứng tuyển không có đề nghị đang hoạt động.");
        }

        if (application.Offer.Status != OfferStatus.Accepted)
        {
            throw new Exception($"Không thể xác nhận tuyển dụng. Trạng thái đề nghị là '{application.Offer.Status}', yêu cầu '{OfferStatus.Accepted}'.");
        }

        // 7. Validate not already hired
        if (ApplicationStage.IsHired(application.Stage))
        {
            throw new Exception("Hồ sơ ứng tuyển này đã được xác nhận tuyển dụng.");
        }

        // 8. Check if the corporate email is already in use
        var existingUser = await _userManager.FindByEmailAsync(request.EmployeeEmail);
        if (existingUser != null)
        {
            throw new Exception("Email đã được sử dụng. Vui lòng sử dụng email khác.");
        }

        // 9. Begin transaction
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 10. Load enterprise for EmployeeCode generation
            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, cancellationToken)
                ?? throw new Exception("Không tìm thấy doanh nghiệp.");

            // 11. Generate a secure password
            var generatedPassword = GenerateSecurePassword();
            var candidateUser = application.Candidate.User;
            var externalCandidate = application.ExternalCandidate;
            var employeeFullName = application.ExternalCandidateId != null && externalCandidate != null
                ? externalCandidate.FullName
                : candidateUser.FullName;
            var employeePhone = application.ExternalCandidateId != null && externalCandidate != null
                ? externalCandidate.PhoneNumber
                : candidateUser.PhoneNumber;
            var welcomeEmail = application.ExternalCandidateId != null && externalCandidate != null
                ? externalCandidate.Email
                : candidateUser.Email;

            // 12. Create new User account with corporate email
            var newUser = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = request.EmployeeEmail,
                Email = request.EmployeeEmail,
                FullName = employeeFullName,
                PhoneNumber = employeePhone,
                EmailConfirmed = true,
                DepartmentId = application.JobPosting.DepartmentId,
                DateJoined = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newUser, generatedPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Tạo tài khoản thất bại: {errors}");
            }

            // 13. Assign Employee role
            await _userManager.AddToRoleAsync(newUser, AppRoles.Employee);

            // 14. Generate EmployeeCode
            var employeeCount = await _context.Employees
                .CountAsync(e => e.EnterpriseId == enterpriseId, cancellationToken);
            var employeeCode = $"{enterprise.EnterpriseCode}-{(employeeCount + 1):D4}";

            // 15. Create Employee record
            var employee = new Employee
            {
                Id = Guid.CreateVersion7(),
                UserId = newUser.Id,
                EnterpriseId = enterpriseId,
                DepartmentId = application.JobPosting.DepartmentId,
                EmployeeCode = employeeCode,
                Position = application.Offer.Position,
                Salary = application.Offer.Salary,
                HireDate = application.Offer.StartDate,
                EmploymentType = "FullTime",
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);

            // 16. Update Application stage to Hired
            application.Stage = ApplicationStage.Hired;
            application.StageUpdatedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Application {ApplicationId} confirmed as hired. Employee {EmployeeCode} created with email {Email} in enterprise {EnterpriseId}.",
                application.Id,
                employeeCode,
                request.EmployeeEmail,
                enterpriseId);

            // 17. Send credentials email (after commit, so email failure won't rollback)
            if (!string.IsNullOrWhiteSpace(welcomeEmail))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        welcomeEmail,
                        "Chào mừng bạn đến với công ty - Thông tin tài khoản",
                        CreateWelcomeEmailTemplate(employeeFullName, request.EmployeeEmail, generatedPassword, employeeCode));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send credentials email to {CandidateEmail} for employee {EmployeeCode}. The hire was still committed successfully.",
                        welcomeEmail,
                        employeeCode);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Skipped credentials email for application {ApplicationId} because candidate email is missing.",
                    application.Id);
            }

            return new ConfirmHireResult
            {
                ApplicationId = application.Id,
                EmployeeId = employee.Id,
                EmployeeCode = employeeCode,
                NewStage = ApplicationStage.Hired,
                EmployeeEmail = request.EmployeeEmail
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Generates a 12-character secure random password with uppercase, lowercase, digit, and special character.
    /// </summary>
    private static string GenerateSecurePassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%&*";
        const string allChars = uppercase + lowercase + digits + special;

        var password = new char[12];

        // Guarantee at least one of each category
        password[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
        password[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        // Fill the rest randomly
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        // Shuffle to avoid predictable positions
        var span = password.AsSpan();
        for (int i = span.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (span[i], span[j]) = (span[j], span[i]);
        }

        return new string(password);
    }

    private static string CreateWelcomeEmailTemplate(string fullName, string email, string password, string employeeCode)
    {
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thông tin tài khoản - ERMS</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
        body {{
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 0;
            background-color: #f6f8ff;
            color: #1a1e36;
            -webkit-font-smoothing: antialiased;
        }}
        .wrapper {{
            width: 100%;
            background-color: #f6f8ff;
            padding: 60px 20px;
        }}
        .container {{
            max-width: 560px;
            margin: 0 auto;
            background-color: #ffffff;
            border: 1px solid #e0e4f2;
            border-radius: 20px;
            overflow: hidden;
            box-shadow: 0 10px 25px rgba(79, 70, 229, 0.05);
        }}
        .accent-bar {{
            height: 6px;
            background: linear-gradient(90deg, #4f46e5 0%, #7c3aed 100%);
        }}
        .brand-section {{
            padding: 40px 45px 15px 45px;
            text-align: left;
        }}
        .brand-logo {{
            font-size: 32px;
            font-weight: 700;
            color: #7c3aed;
            letter-spacing: 1px;
        }}
        .main-content {{
            padding: 15px 45px 45px 45px;
        }}
        .title {{
            font-size: 26px;
            font-weight: 700;
            color: #1e1b4b;
            margin: 0 0 18px 0;
            letter-spacing: -0.5px;
        }}
        .greeting {{
            font-size: 17px;
            font-weight: 600;
            color: #312e81;
            margin-bottom: 12px;
        }}
        .text {{
            font-size: 15px;
            color: #4b5563;
            margin-bottom: 30px;
        }}
        .info-box {{
            background-color: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            padding: 20px;
            margin-bottom: 30px;
        }}
        .info-item {{
            margin-bottom: 15px;
        }}
        .info-item:last-child {{
            margin-bottom: 0;
        }}
        .info-label {{
            font-size: 13px;
            color: #64748b;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 4px;
            display: block;
        }}
        .info-value {{
            font-size: 16px;
            color: #1e293b;
            font-weight: 500;
            word-break: break-all;
        }}
        .password-value {{
            font-family: monospace;
            font-size: 18px;
            color: #4f46e5;
            letter-spacing: 1px;
        }}
        .cta-section {{
            text-align: center;
            margin: 35px 0;
        }}
        .security-note {{
            background-color: #fffbfa;
            border-radius: 12px;
            padding: 10px 14px;
            font-size: 13px;
            color: #b45309;
            border-left: 4px solid #f59e0b;
        }}
        .security-item {{
            margin-bottom: 0;
            display: flex;
            align-items: flex-start;
            gap: 12px;
        }}
        .icon {{
            color: #f59e0b;
            font-size: 14px;
        }}
        .footer {{
            text-align: center;
            padding: 40px 45px;
            background-color: #fcfdfe;
            border-top: 1px solid #f1f5f9;
        }}
        .footer-text {{
            font-size: 12px;
            color: #94a3b8;
            line-height: 2;
        }}
        .copyright {{
            font-size: 11px;
            color: #cbd5e1;
            font-weight: 500;
            margin-top: 20px;
        }}
    </style>
</head>
<body>
    <div class='wrapper'>
        <div class='container'>
            <div class='accent-bar'></div>
            <div class='brand-section'>
                <div class='brand-logo'>ERMS</div>
            </div>

            <div class='main-content'>
                <h1 class='title'>Chào mừng đến với công ty!</h1>

                <p class='greeting'>Chào {fullName},</p>

                <p class='text'>
                    Chúc mừng bạn đã chính thức trở thành nhân viên! Tài khoản hệ thống của bạn đã được thiết lập thành công. Dưới đây là thông tin đăng nhập dành cho bạn:
                </p>

                <div class='info-box'>
                    <div class='info-item'>
                        <span class='info-label'>Mã Nhân Viên</span>
                        <div class='info-value'>{employeeCode}</div>
                    </div>
                    <div class='info-item'>
                        <span class='info-label'>Email / Tên đăng nhập</span>
                        <div class='info-value'>{email}</div>
                    </div>
                    <div class='info-item'>
                        <span class='info-label'>Mật khẩu tạm thời</span>
                        <div class='info-value password-value'>{password}</div>
                    </div>
                </div>

                <div class='security-note'>
                    <div class='security-item'>
                        <span class='icon'>⚠️</span>
                        <span>Vì lý do bảo mật, vui lòng đăng nhập và đổi mật khẩu ngay trong lần truy cập đầu tiên.</span>
                    </div>
                </div>

                <p style='margin-top: 35px; font-size: 14px; color: #64748b;'>
                    Trân trọng,<br>
                    <strong style='color: #4f46e5;'>Phòng Hành chính - Nhân sự (HR)</strong>
                </p>
            </div>

            <div class='footer'>
                <div class='footer-text'>
                    Email này chứa thông tin bảo mật, vui lòng không chia sẻ cho bất kỳ ai.<br>
                    Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với bộ phận IT hoặc HR.
                </div>
                <div class='copyright'>&copy; 2026 ERMS System. All rights reserved.</div>
            </div>
        </div>
    </div>
</body>
</html>";
    }
}
