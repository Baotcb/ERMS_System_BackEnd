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
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: HRManager ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có thể thực hiện hành động này.");
        }

        // 3. Enterprise scoping
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("User is not associated with any enterprise.");

        // 4. Load Application with related entities
        var application = await _context.Applications
            .Include(a => a.JobPosting)
            .Include(a => a.Offer)
            .Include(a => a.Candidate)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, cancellationToken)
            ?? throw new Exception($"Application with ID {request.ApplicationId} not found.");

        // 5. Validate enterprise ownership
        if (application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hồ sơ này.");
        }

        // 6. Validate offer exists and is accepted
        if (application.Offer == null || application.Offer.IsDeleted)
        {
            throw new Exception("Application does not have an active offer.");
        }

        if (application.Offer.Status != OfferStatus.Accepted)
        {
            throw new Exception($"Cannot confirm hire. Offer status is '{application.Offer.Status}', expected '{OfferStatus.Accepted}'.");
        }

        // 7. Validate not already hired
        if (ApplicationStage.IsHired(application.Stage))
        {
            throw new Exception("This application has already been confirmed as hired.");
        }

        // 8. Check if the corporate email is already in use
        var existingUser = await _userManager.FindByEmailAsync(request.EmployeeEmail);
        if (existingUser != null)
        {
            throw new Exception("Email đã được sử dụng. Vui lòng sử dụng email khác.");
        }

        // 9. Begin transaction
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 10. Load enterprise for EmployeeCode generation
            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, cancellationToken)
                ?? throw new Exception("Enterprise not found.");

            // 11. Generate a secure password
            var generatedPassword = GenerateSecurePassword();

            // 12. Create new User account with corporate email
            var candidateUser = application.Candidate.User;
            var newUser = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = request.EmployeeEmail,
                Email = request.EmployeeEmail,
                FullName = candidateUser.FullName,
                PhoneNumber = candidateUser.PhoneNumber,
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
                application.Id, employeeCode, request.EmployeeEmail, enterpriseId);

            // 17. Send credentials email (after commit, so email failure won't rollback)
            try
            {
                await _emailService.SendEmailAsync(
                    candidateUser.Email,
                    "Chào mừng bạn đến với công ty - Thông tin tài khoản",
                    $"Xin chào {candidateUser.FullName},\n\n" +
                    $"Chúc mừng bạn đã chính thức trở thành nhân viên!\n\n" +
                    $"Thông tin tài khoản của bạn:\n" +
                    $"- Email: {request.EmployeeEmail}\n" +
                    $"- Mật khẩu: {generatedPassword}\n\n" +
                    $"Mã nhân viên: {employeeCode}\n\n" +
                    $"Vui lòng đổi mật khẩu sau khi đăng nhập lần đầu.\n\n" +
                    $"Trân trọng.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to send credentials email to {CandidateEmail} for employee {EmployeeCode}. The hire was still committed successfully.",
                    candidateUser.Email, employeeCode);
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
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (span[i], span[j]) = (span[j], span[i]);
        }

        return new string(password);
    }
}
