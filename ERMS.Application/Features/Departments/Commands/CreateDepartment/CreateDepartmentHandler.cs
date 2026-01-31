using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Departments.Commands.CreateDepartment
{
    public sealed class CreateDepartmentHandler : IRequestHandler<CreateDepartmentCommand, int>
    {
        private readonly IERMSDbContext _context;
        private readonly ILogger<CreateDepartmentHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public CreateDepartmentHandler(IERMSDbContext context, ILogger<CreateDepartmentHandler> logger, ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<int> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) throw new UnauthorizedAccessException("User not belong to enterprise");

            // Validate enterprise exists
            var enterpriseExists = await _context.Enterprises
                .AnyAsync(e => e.Id == enterpriseId && !e.IsDeleted, cancellationToken);


            if (!enterpriseExists)
            {
                throw new Exception("Doanh nghiệp không tồn tại");
            }

            // Check duplicate code within enterprise
            if (!string.IsNullOrEmpty(request.DepartmentCode))
            {
                var codeExists = await _context.Departments
                    .AnyAsync(d => d.EnterpriseId == enterpriseId 
                                && d.DepartmentCode == request.DepartmentCode 
                                && !d.IsDeleted, cancellationToken);

                if (codeExists)
                {
                    throw new Exception("Mã phòng ban đã tồn tại trong doanh nghiệp");
                }
            }

            // Validate parent department if provided
            if (request.ParentDepartmentId.HasValue)
            {
                var parentExists = await _context.Departments
                    .AnyAsync(d => d.Id == request.ParentDepartmentId.Value 
                                && d.EnterpriseId == enterpriseId 
                                && !d.IsDeleted, cancellationToken);

                if (!parentExists)
                {
                    throw new Exception("Phòng ban cha không tồn tại");
                }
            }

            var department = new Department
            {
                EnterpriseId = enterpriseId.Value,
                DepartmentName = request.DepartmentName,
                DepartmentCode = request.DepartmentCode,
                Description = request.Description,
                ManagerId = request.ManagerId,
                ParentDepartmentId = request.ParentDepartmentId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created department {DepartmentName} (ID: {DepartmentId}) for enterprise {EnterpriseId}",
                department.DepartmentName, department.Id, department.EnterpriseId);

            return department.Id;
        }
    }
}
