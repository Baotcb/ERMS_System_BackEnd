using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Queries.GetEmployeeDetail
{
    public sealed class GetEmployeeDetailHandler : IRequestHandler<GetEmployeeDetailQuery, EmployeeDetailDto?>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<User>? _userManager;

        public GetEmployeeDetailHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            UserManager<User>? userManager = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _userManager = userManager;
        }

        public async Task<EmployeeDetailDto?> Handle(GetEmployeeDetailQuery request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null)
            {
                throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
            }

            var employee = await _context.Employees
                .Where(e => e.Id == request.Id && e.EnterpriseId == enterpriseId.Value && !e.IsDeleted)
                .Select(e => new
                {
                    e.Id,
                    e.EmployeeCode,
                    FullName = e.User.FullName,
                    Email = e.User.Email!,
                    Phone = e.User.PhoneNumber,
                    e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : null,
                    e.Position,
                    e.SkillDescription,
                    e.EmploymentType,
                    e.HireDate,
                    e.Status,
                    e.CreatedAt,
                    e.ManagerId,
                    User = e.User
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (employee == null)
            {
                return null;
            }

            var roles = new List<string>();
            if (_userManager != null)
            {
                var userRoles = await _userManager.GetRolesAsync(employee.User);
                if (userRoles != null)
                {
                    roles = userRoles.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
                }
            }

            return new EmployeeDetailDto
            {
                Id = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                Email = employee.Email,
                Phone = employee.Phone,
                DepartmentId = employee.DepartmentId,
                DepartmentName = employee.DepartmentName,
                Position = employee.Position,
                SkillDescription = employee.SkillDescription,
                EmploymentType = employee.EmploymentType,
                HireDate = employee.HireDate,
                Status = employee.Status,
                CreatedAt = employee.CreatedAt,
                ManagerId = employee.ManagerId,
                Roles = roles
            };
        }
    }
}
