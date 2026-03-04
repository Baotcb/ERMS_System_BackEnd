using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Departments.Commands.DeleteDepartment
{
    public sealed class DeleteDepartmentHandler : IRequestHandler<DeleteDepartmentCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ILogger<DeleteDepartmentHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public DeleteDepartmentHandler(IERMSDbContext context, ILogger<DeleteDepartmentHandler> logger, ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) throw new UnauthorizedAccessException("User not belong to enterprise");

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == request.Id 
                                       && d.EnterpriseId == enterpriseId 
                                       && !d.IsDeleted, cancellationToken);

            if (department == null)
            {
                throw new Exception("Phòng ban không tồn tại");
            }

            // Check if department has employees
            var hasEmployees = await _context.Employees
                .AnyAsync(e => e.DepartmentId == request.Id && !e.IsDeleted, cancellationToken);

            if (hasEmployees)
            {
                throw new Exception("Không thể xóa phòng ban đang có nhân viên");
            }

            // Check if department has child departments
            var hasChildren = await _context.Departments
                .AnyAsync(d => d.ParentDepartmentId == request.Id && !d.IsDeleted, cancellationToken);

            if (hasChildren)
            {
                throw new Exception("Không thể xóa phòng ban đang có phòng ban con");
            }

            // Soft delete
            department.IsDeleted = true;
            department.DeletedAt = DateTime.UtcNow;
            department.IsActive = false;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted department {DepartmentId}", department.Id);

            return true;
        }
    }
}
