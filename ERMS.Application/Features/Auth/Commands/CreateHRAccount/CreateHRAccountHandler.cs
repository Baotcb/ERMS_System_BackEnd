using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        private readonly IERMSDbContext _context;

        public CreateHRAccountHandler(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IERMSDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<Guid> Handle(CreateHRAccountCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate Enterprise
            var enterprise = await _context.Enterprises
                .AsNoTracking()
                .Include(x => x.SubscriptionPlan)
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(x => x.Id == request.EnterpriseId && !x.IsDeleted, cancellationToken);

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

            // 5. Get or Create HR Department
            var hrDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.EnterpriseId == enterprise.Id && d.DepartmentCode == "HR" && !d.IsDeleted, cancellationToken);

            if (hrDepartment == null)
            {
                hrDepartment = new Department
                {
                    EnterpriseId = enterprise.Id,
                    DepartmentName = "Human Resources",
                    DepartmentCode = "HR",
                    Description = "Default HR Department",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Departments.Add(hrDepartment);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // 6. Generate Employee Code
            var employeeCount = await _context.Employees.CountAsync(e => e.EnterpriseId == enterprise.Id, cancellationToken);
            var employeeCode = $"{enterprise.EnterpriseCode}-{(employeeCount + 1):D4}";

            // 7. Create Employee Record
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

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}
