using ERMS.Application.Features.JobPostings.Queries.PublicJobFiltering;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobFilterOptions;

public sealed class GetPublicJobFilterOptionsHandler
    : IRequestHandler<GetPublicJobFilterOptionsQuery, GetPublicJobFilterOptionsResponse>
{
    private readonly IERMSDbContext _context;

    public GetPublicJobFilterOptionsHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetPublicJobFilterOptionsResponse> Handle(
        GetPublicJobFilterOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var jobs = _context.JobPostings
            .AsNoTracking()
            .Where(jp => jp.Status == JobPostingStatus.Published
                      && !jp.IsDeleted
                      && jp.Enterprise.Status == "Active"
                      && !jp.Enterprise.IsDeleted
                      && (!jp.ApplicationDeadline.HasValue || jp.ApplicationDeadline.Value >= utcNow));

        var departments = await jobs
            .GroupBy(jp => new
            {
                jp.DepartmentId,
                DepartmentName = jp.Department.DepartmentName
            })
            .OrderBy(group => group.Key.DepartmentName)
            .Select(group => new PublicDepartmentFilterDto
            {
                Id = group.Key.DepartmentId,
                DepartmentName = group.Key.DepartmentName,
                JobCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        var locations = await jobs
            .Select(jp => jp.Location == null ? null : jp.Location.Trim())
            .Where(location => location != null && location != string.Empty)
            .Distinct()
            .OrderBy(location => location)
            .ToListAsync(cancellationToken);

        return new GetPublicJobFilterOptionsResponse
        {
            Departments = departments,
            Locations = locations.Where(location => location != null).Cast<string>().ToList(),
            EmploymentTypes = PublicJobFilterDefinitions.EmploymentTypes.ToList(),
            ExperienceBuckets = PublicJobFilterDefinitions.ExperienceBuckets.ToList(),
            SalaryBuckets = PublicJobFilterDefinitions.SalaryBuckets.ToList()
        };
    }
}
