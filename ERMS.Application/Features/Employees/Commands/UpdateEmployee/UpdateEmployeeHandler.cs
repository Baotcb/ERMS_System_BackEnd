using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Commands.UpdateEmployee
{
    public sealed class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, bool>
    {
        private static readonly HashSet<string> ManagedRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            AppRoles.Employee,
            AppRoles.Trainer,
            AppRoles.DepartmentHead,
            AppRoles.Director
        };

        private readonly IERMSDbContext _context;
        private readonly ILogger<UpdateEmployeeHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<User>? _userManager;

        public UpdateEmployeeHandler(
            IERMSDbContext context,
            ILogger<UpdateEmployeeHandler> logger,
            ICurrentUserService currentUserService,
            UserManager<User>? userManager = null)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
            _userManager = userManager;
        }

        public async Task<bool> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null)
            {
                throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.Id
                                       && e.EnterpriseId == enterpriseId
                                       && !e.IsDeleted, cancellationToken);

            if (employee == null)
            {
                throw new Exception("Nhân viên không tồn tại");
            }

            var normalizedRole = NormalizeRole(request.Role);
            if (string.IsNullOrWhiteSpace(request.Role))
            {
                if (_userManager == null)
                {
                    normalizedRole = AppRoles.Employee;
                }
                else
                {
                    throw new Exception("Vai trò là bắt buộc");
                }
            }
            else if (normalizedRole == null)
            {
                throw new Exception("Vai trò không hợp lệ");
            }

            if (normalizedRole == null)
            {
                throw new Exception("Vai trò không hợp lệ");
            }

            var isDirectorRole = string.Equals(normalizedRole, AppRoles.Director, StringComparison.OrdinalIgnoreCase);
            if (!isDirectorRole)
            {
                if (!request.DepartmentId.HasValue)
                {
                    throw new Exception("Phòng ban là bắt buộc với vai trò này");
                }

                var departmentExists = await _context.Departments
                    .AnyAsync(d => d.Id == request.DepartmentId
                                && d.EnterpriseId == enterpriseId
                                && !d.IsDeleted, cancellationToken);

                if (!departmentExists)
                {
                    throw new Exception("Phòng ban không tồn tại");
                }
            }

            employee.DepartmentId = isDirectorRole ? null : request.DepartmentId;
            employee.Position = request.Position;
            employee.EmploymentType = request.EmploymentType;
            employee.ManagerId = request.ManagerId;
            employee.Status = request.Status;
            employee.UpdatedAt = DateTime.UtcNow;

            if (_userManager != null)
            {
                await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
                try
                {
                    await SyncManagedRoleAsync(employee.UserId, normalizedRole);
                    await _context.SaveChangesAsync(cancellationToken);
                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
                catch
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    throw;
                }
            }
            else
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Updated employee {EmployeeId}", employee.Id);
            return true;
        }

        private async Task SyncManagedRoleAsync(Guid userId, string targetRole)
        {
            if (_userManager == null)
            {
                throw new InvalidOperationException("UserManager is not configured for role synchronization.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new Exception("Không tìm thấy tài khoản người dùng của nhân viên.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in currentRoles.Where(r => ManagedRoles.Contains(r) && !string.Equals(r, targetRole, StringComparison.OrdinalIgnoreCase)))
            {
                var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, role);
                if (!removeRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", removeRoleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Không thể gỡ vai trò {role}: {errors}");
                }
            }

            if (!currentRoles.Any(r => string.Equals(r, targetRole, StringComparison.OrdinalIgnoreCase)))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, targetRole);
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Không thể gán vai trò {targetRole}: {errors}");
                }
            }
        }

        private static string? NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            return ManagedRoles.FirstOrDefault(r =>
                string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }
    }
}
