using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Departments.Commands.UpdateDepartment
{
    public sealed class UpdateDepartmentHandler : IRequestHandler<UpdateDepartmentCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ILogger<UpdateDepartmentHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public UpdateDepartmentHandler(IERMSDbContext context, ILogger<UpdateDepartmentHandler> logger, ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == request.Id 
                                       && d.EnterpriseId == enterpriseId 
                                       && !d.IsDeleted, cancellationToken);

            if (department == null)
            {
                throw new Exception("Phòng ban không tồn tại");
            }

            // Check duplicate code within enterprise (exclude current)
            if (!string.IsNullOrEmpty(request.DepartmentCode))
            {
                var codeExists = await _context.Departments
                    .AnyAsync(d => d.EnterpriseId == enterpriseId 
                                && d.DepartmentCode == request.DepartmentCode 
                                && d.Id != request.Id
                                && !d.IsDeleted, cancellationToken);

                if (codeExists)
                {
                    throw new Exception("Mã phòng ban đã tồn tại trong doanh nghiệp");
                }
            }

            // Validate parent department
            if (request.ParentDepartmentId.HasValue)
            {
                if (request.ParentDepartmentId.Value == request.Id)
                {
                    throw new Exception("Phòng ban không thể là cha của chính nó");
                }

                var parentExists = await _context.Departments
                    .AnyAsync(d => d.Id == request.ParentDepartmentId.Value 
                                && d.EnterpriseId == enterpriseId 
                                && !d.IsDeleted, cancellationToken);

                if (!parentExists)
                {
                    throw new Exception("Phòng ban cha không tồn tại");
                }
            }

            department.DepartmentName = request.DepartmentName;
            department.DepartmentCode = request.DepartmentCode;
            department.Description = request.Description;
            department.ManagerId = request.ManagerId;
            department.ParentDepartmentId = request.ParentDepartmentId;
            department.IsActive = request.IsActive;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã cập nhật phòng ban {DepartmentId}", department.Id);

            return true;
        }
    }
}
