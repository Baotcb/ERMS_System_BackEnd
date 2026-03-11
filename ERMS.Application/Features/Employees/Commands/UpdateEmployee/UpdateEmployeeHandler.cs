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
        private readonly IERMSDbContext _context;
        private readonly ILogger<UpdateEmployeeHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<User> _userManager;

        private static readonly HashSet<string> ValidEmploymentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "FullTime", "PartTime", "Contract", "Intern"
        };

        private static readonly Dictionary<string, string> EmploymentTypeAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["FullTime"] = "FullTime",
            ["Full-time"] = "FullTime",
            ["Full Time"] = "FullTime",
            ["PartTime"] = "PartTime",
            ["Part-time"] = "PartTime",
            ["Part Time"] = "PartTime",
            ["Contract"] = "Contract",
            ["Intern"] = "Intern"
        };

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Active", "Inactive", "OnLeave", "Terminated"
        };

        private static readonly Dictionary<string, string> StatusAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Active"] = "Active",
            ["Inactive"] = "Inactive",
            ["OnLeave"] = "OnLeave",
            ["On Leave"] = "OnLeave",
            ["On-leave"] = "OnLeave",
            ["Terminated"] = "Terminated"
        };

        // Các role có thể gán qua form edit
        private static readonly HashSet<string> AssignableRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            AppRoles.Employee, AppRoles.Trainer, AppRoles.DepartmentHead
        };

        // Các role nhân sự do màn hình edit quản lý
        private static readonly HashSet<string> ManagedEmployeeRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            AppRoles.Employee, AppRoles.Trainer, AppRoles.DepartmentHead, AppRoles.Director, AppRoles.HRManager
        };

        private static readonly HashSet<string> PrivilegedRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            AppRoles.Director, AppRoles.HRManager
        };

        public UpdateEmployeeHandler(
            IERMSDbContext context,
            ILogger<UpdateEmployeeHandler> logger,
            ICurrentUserService currentUserService,
            UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
            _userManager = userManager;
        }

        public async Task<bool> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            var departmentId = await _currentUserService.GetDepartmentIdAsync();
            if (enterpriseId == null) throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

            var normalizedEmploymentType = NormalizeEmploymentType(request.EmploymentType);

            if (!ValidEmploymentTypes.Contains(normalizedEmploymentType))
            {
                throw new Exception("Loại hợp đồng không hợp lệ. Giá trị hợp lệ: FullTime, PartTime, Contract, Intern.");
            }

            var normalizedStatus = NormalizeStatus(request.Status);

            if (!ValidStatuses.Contains(normalizedStatus))
            {
                throw new Exception("Trạng thái không hợp lệ. Giá trị hợp lệ: Active, Inactive, OnLeave, Terminated.");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.Id
                                       && e.EnterpriseId == enterpriseId
                                       && !e.IsDeleted, cancellationToken);

            if (employee == null)
            {
                throw new Exception("Nhân viên không tồn tại");
            }

            var departmentExists = await _context.Departments
                .AnyAsync(d => d.Id == departmentId
                            && d.EnterpriseId == enterpriseId
                            && !d.IsDeleted, cancellationToken);

            if (!departmentExists)
            {
                throw new Exception("Phòng ban không tồn tại");
            }

            if (request.ManagerId.HasValue)
            {
                if (request.ManagerId.Value == request.Id)
                {
                    throw new Exception("Nhân viên không thể tự quản lý chính mình");
                }

                // Chỉ validate manager khi ManagerId thay đổi so với giá trị hiện tại
                // Tránh chặn update subordinate khi manager đã bị soft-delete
                if (request.ManagerId.Value != employee.ManagerId)
                {
                    var managerExists = await _context.Employees
                        .AnyAsync(e => e.Id == request.ManagerId.Value
                                    && e.EnterpriseId == enterpriseId
                                    && !e.IsDeleted, cancellationToken);

                    if (!managerExists)
                    {
                        throw new Exception("Quản lý trực tiếp không tồn tại");
                    }
                }
            }

            // Dùng transaction để đảm bảo role changes + employee update atomic
            await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {
                employee.DepartmentId = departmentId;
                employee.Position = request.Position;
                employee.EmploymentType = normalizedEmploymentType;
                employee.ManagerId = request.ManagerId;
                employee.Status = normalizedStatus;
                employee.UpdatedAt = DateTime.UtcNow;

                // Cập nhật role nếu được cung cấp
                if (!string.IsNullOrWhiteSpace(request.Role))
                {
                    var user = await _userManager.FindByIdAsync(employee.UserId.ToString());
                    if (user == null)
                    {
                        throw new Exception("Không tìm thấy tài khoản người dùng của nhân viên.");
                    }

                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var normalizedRequestedRole = ResolveManagedRole(request.Role);
                    var isKeepingExistingManagedRole = currentRoles.Any(r =>
                        ManagedEmployeeRoles.Contains(r) &&
                        string.Equals(r, normalizedRequestedRole, StringComparison.OrdinalIgnoreCase));

                    if (!AssignableRoles.Contains(normalizedRequestedRole) && !isKeepingExistingManagedRole)
                    {
                        throw new Exception("Vai trò không hợp lệ. Giá trị hợp lệ: Employee, Trainer, DepartmentHead.");
                    }

                    var privilegedRolesBeingRemoved = currentRoles
                        .Where(r => PrivilegedRoles.Contains(r) &&
                                    !string.Equals(r, normalizedRequestedRole, StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (privilegedRolesBeingRemoved.Any() &&
                        !_currentUserService.Roles.Contains(AppRoles.Director, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception("Chỉ Director mới có thể thay đổi vai trò của HR Manager hoặc Director.");
                    }

                    foreach (var privilegedRole in privilegedRolesBeingRemoved)
                    {
                        var hasAnotherHolder = await HasAnotherEmployeeWithRoleAsync(
                            enterpriseId.Value,
                            employee.Id,
                            privilegedRole,
                            cancellationToken);

                        if (!hasAnotherHolder)
                        {
                            throw new Exception($"Không thể gỡ vai trò {privilegedRole} cuối cùng trong doanh nghiệp.");
                        }
                    }

                    if (!currentRoles.Contains(normalizedRequestedRole, StringComparer.OrdinalIgnoreCase))
                    {
                        var addResult = await _userManager.AddToRoleAsync(user, normalizedRequestedRole);

                        if (!addResult.Succeeded)
                        {
                            var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                            throw new Exception($"Không thể gán vai trò mới: {errors}");
                        }
                    }

                    var updatedRoles = await _userManager.GetRolesAsync(user);

                    employee.IsTrainer = updatedRoles.Contains(AppRoles.Trainer, StringComparer.OrdinalIgnoreCase);

                    _logger.LogInformation("Updated role for employee {EmployeeId} to {Role}", employee.Id, normalizedRequestedRole);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Updated employee {EmployeeId}", employee.Id);

            return true;
        }

        private static string NormalizeEmploymentType(string employmentType)
        {
            if (string.IsNullOrWhiteSpace(employmentType))
            {
                return employmentType;
            }

            var trimmedEmploymentType = employmentType.Trim();
            return EmploymentTypeAliases.TryGetValue(trimmedEmploymentType, out var normalizedEmploymentType)
                ? normalizedEmploymentType
                : trimmedEmploymentType;
        }

        private static string NormalizeStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return status;
            }

            var trimmedStatus = status.Trim();
            return StatusAliases.TryGetValue(trimmedStatus, out var normalizedStatus)
                ? normalizedStatus
                : trimmedStatus;
        }

        private static string ResolveManagedRole(string role)
        {
            return ManagedEmployeeRoles.FirstOrDefault(r => string.Equals(r, role?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? role;
        }

        private async Task<bool> HasAnotherEmployeeWithRoleAsync(
            Guid enterpriseId,
            Guid excludedEmployeeId,
            string role,
            CancellationToken cancellationToken)
        {
            var otherUserIds = await _context.Employees
                .Where(e => e.EnterpriseId == enterpriseId
                            && e.Id != excludedEmployeeId
                            && !e.IsDeleted)
                .Select(e => e.UserId)
                .ToListAsync(cancellationToken);

            if (!otherUserIds.Any())
            {
                return false;
            }

            var otherUsers = await _context.Users
                .Where(u => otherUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            foreach (var otherUser in otherUsers)
            {
                var userRoles = await _userManager.GetRolesAsync(otherUser);
                if (userRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
