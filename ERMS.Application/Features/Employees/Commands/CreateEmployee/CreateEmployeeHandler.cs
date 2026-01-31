using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Commands.CreateEmployee
{
    public sealed class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<CreateEmployeeHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public CreateEmployeeHandler(
            IERMSDbContext context,
            UserManager<User> userManager,
            ILogger<CreateEmployeeHandler> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) throw new UnauthorizedAccessException("User not belong to enterprise");

            // Validate enterprise exists
            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, cancellationToken);

            if (enterprise == null)
            {
                throw new Exception("Doanh nghiệp không tồn tại");
            }

            // Validate department exists
            var departmentExists = await _context.Departments
                .AnyAsync(d => d.Id == request.DepartmentId 
                            && d.EnterpriseId == enterpriseId 
                            && !d.IsDeleted, cancellationToken);

            if (!departmentExists)
            {
                throw new Exception("Phòng ban không tồn tại");
            }

            // Check email already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new Exception("Email đã được sử dụng");
            }

            // Create User account
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.Phone,
                EmailConfirmed = true, // HR created, no need email confirmation
                DateJoined = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Tạo tài khoản thất bại: {errors}");
            }

            // Assign Employee role
            await _userManager.AddToRoleAsync(user, AppRoles.Employee);

            // Generate employee code
            var employeeCount = await _context.Employees
                .CountAsync(e => e.EnterpriseId == enterpriseId, cancellationToken);
            var employeeCode = $"{enterprise.EnterpriseCode}-{(employeeCount + 1):D4}";

            // Create Employee record
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EnterpriseId = enterpriseId.Value,
                DepartmentId = request.DepartmentId,
                EmployeeCode = employeeCode,
                Position = request.Position,
                EmploymentType = request.EmploymentType,
                HireDate = request.HireDate ?? DateTime.UtcNow,
                ManagerId = request.ManagerId,
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created employee {EmployeeCode} for user {Email} in enterprise {EnterpriseId}",
                employee.EmployeeCode, user.Email, employee.EnterpriseId);

            return employee.Id;
        }
    }
}
