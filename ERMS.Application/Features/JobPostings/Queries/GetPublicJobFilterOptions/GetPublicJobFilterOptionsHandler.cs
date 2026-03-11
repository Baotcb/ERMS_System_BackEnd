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
        var jobs = await _context.JobPostings
            .AsNoTracking()
            .Where(jp => jp.Status == JobPostingStatus.Published
                      && !jp.IsDeleted
                      && jp.Enterprise.Status == "Active"
                      && !jp.Enterprise.IsDeleted
                      && (!jp.ApplicationDeadline.HasValue || jp.ApplicationDeadline.Value >= DateTime.UtcNow))
            .Select(jp => new
            {
                jp.DepartmentId,
                DepartmentName = jp.Department.DepartmentName,
                jp.Location
            })
            .ToListAsync(cancellationToken);

        return new GetPublicJobFilterOptionsResponse
        {
            Departments = jobs
                .GroupBy(job => new { job.DepartmentId, job.DepartmentName })
                .OrderBy(group => group.Key.DepartmentName)
                .Select(group => new PublicDepartmentFilterDto
                {
                    Id = group.Key.DepartmentId,
                    DepartmentName = group.Key.DepartmentName,
                    JobCount = group.Count()
                })
                .ToList(),
            Locations = jobs
                .Select(job => job.Location?.Trim())
                .Where(location => !string.IsNullOrWhiteSpace(location))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(location => location)
                .Cast<string>()
                .ToList(),
            EmploymentTypes = PublicJobFilterDefinitions.EmploymentTypes.ToList(),
            ExperienceBuckets = PublicJobFilterDefinitions.ExperienceBuckets.ToList(),
            SalaryBuckets = PublicJobFilterDefinitions.SalaryBuckets.ToList()
        };
    }
}
