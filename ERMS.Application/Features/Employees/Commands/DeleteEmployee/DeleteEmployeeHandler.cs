using ERMS.Application.Interface;
using MediatR;
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

        public DeleteEmployeeHandler(IERMSDbContext context, ILogger<DeleteEmployeeHandler> logger, ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) throw new UnauthorizedAccessException("User not belong to enterprise");

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

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted employee {EmployeeId}", employee.Id);

            return true;
        }
    }
}
