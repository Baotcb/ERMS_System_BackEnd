using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Queries.GetAllEmployees
{
    public sealed class GetAllEmployeesHandler : IRequestHandler<GetAllEmployeesQuery, GetAllEmployeesResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAllEmployeesHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetAllEmployeesResult> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null)
            {
                throw new System.UnauthorizedAccessException("User does not belong to any enterprise");
            }

            var query = _context.Employees
                .Where(e => e.EnterpriseId == enterpriseId.Value && !e.IsDeleted)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(e => 
                    e.EmployeeCode.ToLower().Contains(search) ||
                    e.User.FullName.ToLower().Contains(search) ||
                    e.User.Email!.ToLower().Contains(search));
            }

            // Department filter
            if (request.DepartmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);
            }

            // Status filter
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(e => e.Status == request.Status);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(e => e.User.FullName)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.User.FullName,
                    Email = e.User.Email!,
                    Phone = e.User.PhoneNumber,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department.DepartmentName,
                    Position = e.Position,
                    EmploymentType = e.EmploymentType,
                    HireDate = e.HireDate,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new GetAllEmployeesResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
