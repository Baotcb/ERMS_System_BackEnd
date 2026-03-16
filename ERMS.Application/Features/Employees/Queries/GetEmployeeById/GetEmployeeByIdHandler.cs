using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Queries.GetEmployeeById
{
    public sealed class GetEmployeeByIdHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDetailDto>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<User> _userManager;

        public GetEmployeeByIdHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            UserManager<User> userManager)
        {
            _context = context;
            _currentUserService = currentUserService;
            _userManager = userManager;
        }

        public async Task<EmployeeDetailDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null)
            {
                throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
            }

            var employee = await _context.Employees
                .Where(e => e.Id == request.Id
                         && e.EnterpriseId == enterpriseId.Value
                         && !e.IsDeleted)
                .Select(e => new EmployeeDetailDto
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.User.FullName,
                    Email = e.User.Email!,
                    Phone = e.User.PhoneNumber,
                    DepartmentId = e.DepartmentId ?? 0,
                    DepartmentName = e.Department.DepartmentName,
                    Position = e.Position,
                    JobPositionId = e.JobPositionId,
                    HireDate = e.HireDate,
                    TerminationDate = e.TerminationDate,
                    EmploymentType = e.EmploymentType,
                    ManagerId = e.ManagerId,
                    ManagerName = e.Manager != null ? e.Manager.User.FullName : null,
                    Salary = e.Salary,
                    IsTrainer = e.IsTrainer,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (employee == null)
            {
                throw new KeyNotFoundException("Nhân viên không tồn tại");
            }

            // Lấy roles của user qua UserManager
            var user = await _userManager.FindByIdAsync(employee.UserId.ToString());
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                employee.Roles = roles.ToList();
            }

            return employee;
        }
    }
}

