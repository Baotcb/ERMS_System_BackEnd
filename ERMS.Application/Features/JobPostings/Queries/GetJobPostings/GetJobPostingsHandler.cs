using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.JobPostings.Queries.GetJobPostings;

public sealed class GetJobPostingsHandler : IRequestHandler<GetJobPostingsQuery, GetJobPostingsResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetJobPostingsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetJobPostingsResponse> Handle(GetJobPostingsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || (!userRoles.Contains(AppRoles.HRManager) && !userRoles.Contains(AppRoles.Director)))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager hoặc Giám đốc mới có quyền xem tin tuyển dụng.");
        }

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        var query = _context.JobPostings
            .Include(jp => jp.Department)
            .Where(jp => jp.EnterpriseId == enterpriseId && !jp.IsDeleted);

        // Filter by status
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(jp => jp.Status == request.Status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(jp => jp.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(jp => new JobPostingListDto
            {
                Id = jp.Id,
                PlanDetailId = jp.PlanDetailId,
                JobTitle = jp.JobTitle,
                JobCode = jp.JobCode,
                Status = jp.Status,
                DepartmentName = jp.Department.DepartmentName,
                Location = jp.Location,
                Quantity = jp.Quantity,
                ApplicationDeadline = jp.ApplicationDeadline,
                PublishedAt = jp.PublishedAt,
                ApplicationCount = jp.ApplicationCount,
                CreatedAt = jp.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetJobPostingsResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
