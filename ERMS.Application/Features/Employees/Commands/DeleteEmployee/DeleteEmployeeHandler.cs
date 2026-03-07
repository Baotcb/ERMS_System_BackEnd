using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Commands.DeleteEmployee
{
    public sealed class DeleteEmployeeHandler : IRequestHandler<DeleteEmployeeCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ILogger<DeleteEmployeeHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<User> _userManager;

        public DeleteEmployeeHandler(
            IERMSDbContext context,
            ILogger<DeleteEmployeeHandler> logger,
            ICurrentUserService currentUserService,
            UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
            _userManager = userManager;
        }

        public async Task<bool> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.Id 
                                       && e.EnterpriseId == enterpriseId 
                                       && !e.IsDeleted, cancellationToken);

            if (employee == null)
            {
                throw new Exception("Nhân viên không tồn tại");
            }

            // Soft delete employee
            employee.IsDeleted = true;
            employee.DeletedAt = DateTime.UtcNow;
            employee.Status = "Inactive";

            // Lock the associated user account
            var user = await _userManager.FindByIdAsync(employee.UserId.ToString());
            if (user != null)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted employee {EmployeeId}", employee.Id);

            return true;
        }
    }
}
