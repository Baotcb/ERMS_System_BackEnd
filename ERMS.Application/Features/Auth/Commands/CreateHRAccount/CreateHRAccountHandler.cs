using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Auth.Commands.CreateHRAccount
{
    public sealed class CreateHRAccountHandler : IRequestHandler<CreateHRAccountCommand, Guid>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IEnterprisesService _enterpriseService;
        private readonly IDepartmentService _departmentService;
        private readonly IEmployeeService _employeeService;

        public CreateHRAccountHandler(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IEnterprisesService enterpriseService,
            IDepartmentService departmentService,
            IEmployeeService employeeService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _enterpriseService = enterpriseService;
            _departmentService = departmentService;
            _employeeService = employeeService;
        }

        public async Task<Guid> Handle(CreateHRAccountCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate Enterprise (via Service)
            var enterprise = await _enterpriseService.GetByIdAsync(request.EnterpriseId);

            if (enterprise == null || enterprise.IsDeleted)
            {
                throw new InvalidOperationException("Doanh nghiệp không tồn tại.");
            }

            // 2. Check if Email exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email đã được đăng ký.");
            }

            // 3. Create User
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                DateJoined = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Tạo tài khoản thất bại: {errors}");
            }

            // 4. Assign Role (HRManager)
            if (!await _roleManager.RoleExistsAsync(AppRoles.HRManager))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.HRManager));
            }
            await _userManager.AddToRoleAsync(user, AppRoles.HRManager);

            // 5. Get or Create HR Department (via service - Clean Architecture)
            var hrDepartment = await _departmentService.GetOrCreateHRDepartmentAsync(enterprise.Id, cancellationToken);

            // 6. Generate Employee Code (via service)
            var employeeCode = await _employeeService.GenerateEmployeeCodeAsync(enterprise.Id, cancellationToken);

            // 7. Create Employee Record (via service)
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EnterpriseId = enterprise.Id,
                DepartmentId = hrDepartment.Id,
                EmployeeCode = employeeCode,
                Position = "HR Manager",
                EmploymentType = "Full-time",
                IsTrainer = false,
                Status = "Active",
                HireDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _employeeService.CreateAsync(employee, cancellationToken);

            return user.Id;
        }
    }
}
