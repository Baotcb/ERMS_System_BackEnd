using ERMS.Application.Interface;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Employees.Queries.GetAllEmployees
{
    public sealed class GetAllEmployeesHandler : IRequestHandler<GetAllEmployeesQuery, GetAllEmployeesResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAllEmployeesHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetAllEmployeesResult> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null)
            {
                throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
            }

            var query = _context.Employees
                .Where(e => e.EnterpriseId == enterpriseId.Value && !e.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(e =>
                    e.EmployeeCode.ToLower().Contains(search) ||
                    e.User.FullName.ToLower().Contains(search) ||
                    e.User.Email!.ToLower().Contains(search));
            }

            if (request.DepartmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId.HasValue && e.DepartmentId.Value == request.DepartmentId.Value);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(e => e.Status == request.Status);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var pageRows = await query
                .OrderBy(e => e.User.FullName)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(e => new
                {
                    e.Id,
                    e.UserId,
                    e.EmployeeCode,
                    FullName = e.User.FullName,
                    Email = e.User.Email!,
                    Phone = e.User.PhoneNumber,
                    e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : null,
                    e.Position,
                    e.SkillDescription,
                    e.EmploymentType,
                    e.HireDate,
                    e.Status,
                    e.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var rolesByUserId = new Dictionary<Guid, List<string>>();
            if (pageRows.Count > 0 && _context is DbContext dbContext)
            {
                var userIds = pageRows.Select(r => r.UserId).Distinct().ToList();
                var roleRows = await (
                    from userRole in dbContext.Set<IdentityUserRole<Guid>>()
                    join role in dbContext.Set<IdentityRole<Guid>>() on userRole.RoleId equals role.Id
                    where userIds.Contains(userRole.UserId)
                    select new
                    {
                        userRole.UserId,
                        RoleName = role.Name
                    }
                ).ToListAsync(cancellationToken);

                rolesByUserId = roleRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.RoleName))
                    .GroupBy(r => r.UserId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(r => r.RoleName!).Distinct().ToList());
            }

            var items = new List<EmployeeDto>(pageRows.Count);
            foreach (var row in pageRows)
            {
                var roles = rolesByUserId.TryGetValue(row.UserId, out var mappedRoles)
                    ? mappedRoles
                    : new List<string>();

                items.Add(new EmployeeDto
                {
                    Id = row.Id,
                    EmployeeCode = row.EmployeeCode,
                    FullName = row.FullName,
                    Email = row.Email,
                    Phone = row.Phone,
                    DepartmentId = row.DepartmentId,
                    DepartmentName = row.DepartmentName,
                    Position = row.Position,
                    SkillDescription = row.SkillDescription,
                    EmploymentType = row.EmploymentType,
                    HireDate = row.HireDate,
                    Status = row.Status,
                    CreatedAt = row.CreatedAt,
                    Roles = roles
                });
            }

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
