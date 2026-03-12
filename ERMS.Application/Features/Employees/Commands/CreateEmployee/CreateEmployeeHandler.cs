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
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Commands.CreateEmployee
{
    public sealed class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, Guid>
    {
        private static readonly HashSet<string> AssignableRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            AppRoles.Employee,
            AppRoles.Trainer,
            AppRoles.DepartmentHead,
            AppRoles.Director
        };

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
            if (enterpriseId == null)
            {
                throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
            }

            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.Id == enterpriseId && !e.IsDeleted, cancellationToken);

            if (enterprise == null)
            {
                throw new Exception("Doanh nghiệp không tồn tại");
            }

            if (string.IsNullOrWhiteSpace(request.Role))
            {
                throw new Exception("Vai trò là bắt buộc");
            }

            var role = NormalizeRole(request.Role);
            if (role == null)
            {
                throw new Exception("Vai trò không hợp lệ");
            }

            var isDirectorRole = string.Equals(role, AppRoles.Director, StringComparison.OrdinalIgnoreCase);
            if (!isDirectorRole && !request.DepartmentId.HasValue)
            {
                throw new Exception("Phòng ban là bắt buộc với vai trò này");
            }

            if (!isDirectorRole)
            {
                var departmentExists = await _context.Departments
                    .AnyAsync(d => d.Id == request.DepartmentId
                                && d.EnterpriseId == enterpriseId
                                && !d.IsDeleted, cancellationToken);

                if (!departmentExists)
                {
                    throw new Exception("Phòng ban không tồn tại");
                }
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new Exception("Email đã được sử dụng");
            }

            var user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.Phone,
                EmailConfirmed = true,
                DateJoined = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Tạo tài khoản thất bại: {errors}");
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, role);
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                throw new Exception($"Gán vai trò thất bại: {errors}");
            }

            var employeeCount = await _context.Employees
                .CountAsync(e => e.EnterpriseId == enterpriseId, cancellationToken);
            var employeeCode = $"{enterprise.EnterpriseCode}-{(employeeCount + 1):D4}";

            var employee = new Employee
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                EnterpriseId = enterpriseId.Value,
                DepartmentId = isDirectorRole ? null : request.DepartmentId,
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

            _logger.LogInformation(
                "Created employee {EmployeeCode} for user {Email} in enterprise {EnterpriseId}",
                employee.EmployeeCode, user.Email, employee.EnterpriseId);

            return employee.Id;
        }

        private static string? NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            return AssignableRoles.FirstOrDefault(r =>
                string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }
    }
}
