using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Commands.UpdateEmployee
{
    public sealed class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ILogger<UpdateEmployeeHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public UpdateEmployeeHandler(IERMSDbContext context, ILogger<UpdateEmployeeHandler> logger, ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
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

            // Validate department
            var departmentExists = await _context.Departments
                .AnyAsync(d => d.Id == request.DepartmentId 
                            && d.EnterpriseId == enterpriseId 
                            && !d.IsDeleted, cancellationToken);

            if (!departmentExists)
            {
                throw new Exception("Phòng ban không tồn tại");
            }

            employee.DepartmentId = request.DepartmentId;
            employee.Position = request.Position;
            employee.EmploymentType = request.EmploymentType;
            employee.ManagerId = request.ManagerId;
            employee.Status = request.Status;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated employee {EmployeeId}", employee.Id);

            return true;
        }
    }
}
