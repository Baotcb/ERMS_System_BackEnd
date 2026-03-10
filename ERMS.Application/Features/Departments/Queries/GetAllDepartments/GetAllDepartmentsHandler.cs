using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Departments.Queries.GetAllDepartments
{
    public sealed class GetAllDepartmentsHandler : IRequestHandler<GetAllDepartmentsQuery, GetAllDepartmentsResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAllDepartmentsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetAllDepartmentsResult> Handle(GetAllDepartmentsQuery request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null)
            {
                // Return empty or throw? Throwing Unauthorized is better to signal issue
                throw new System.UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
            }

            var query = _context.Departments
                .Where(d => d.EnterpriseId == enterpriseId.Value && !d.IsDeleted)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(d => 
                    d.DepartmentName.ToLower().Contains(search) ||
                    (d.DepartmentCode != null && d.DepartmentCode.ToLower().Contains(search)));
            }

            // Active filter
            if (request.IsActive.HasValue)
            {
                query = query.Where(d => d.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(d => d.DepartmentName)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    DepartmentName = d.DepartmentName,
                    DepartmentCode = d.DepartmentCode,
                    Description = d.Description,
                    ManagerId = d.ManagerId,
                    ManagerName = d.Manager != null ? d.Manager.User.FullName : null,
                    ParentDepartmentId = d.ParentDepartmentId,
                    ParentDepartmentName = d.ParentDepartment != null ? d.ParentDepartment.DepartmentName : null,
                    IsActive = d.IsActive,
                    EmployeeCount = d.Employees.Count(e => !e.IsDeleted),
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new GetAllDepartmentsResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
