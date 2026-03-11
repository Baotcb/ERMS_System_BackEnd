using ERMS.Application.Interface;
using ERMS.Application.Features.JobPostings.Queries.PublicJobFiltering;
using ERMS.Domain.Constants.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostings;

/// <summary>
/// Handler for public job postings query (no authentication required)
/// </summary>
public sealed class GetPublicJobPostingsHandler : IRequestHandler<GetPublicJobPostingsQuery, GetPublicJobPostingsResponse>
{
    private readonly IERMSDbContext _context;

    public GetPublicJobPostingsHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetPublicJobPostingsResponse> Handle(GetPublicJobPostingsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.JobPostings
            .AsNoTracking()
            .Where(jp => jp.Status == JobPostingStatus.Published
                      && !jp.IsDeleted
                      && jp.Enterprise.Status == "Active"
                      && !jp.Enterprise.IsDeleted);

        query = query.Where(jp => !jp.ApplicationDeadline.HasValue 
                               || jp.ApplicationDeadline.Value >= DateTime.UtcNow);

        if (request.EnterpriseId.HasValue)
        {
            query = query.Where(jp => jp.EnterpriseId == request.EnterpriseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim().ToLower();
            query = query.Where(jp => jp.JobTitle.ToLower().Contains(searchTerm)
                                   || jp.Enterprise.EnterpriseName.ToLower().Contains(searchTerm)
                                   || (jp.Description != null && jp.Description.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var location = request.Location.Trim().ToLower();
            query = query.Where(jp => jp.Location != null && jp.Location.ToLower().Contains(location));
        }

        if (!string.IsNullOrWhiteSpace(request.EmploymentType))
        {
            var normalizedEmploymentType = PublicJobFilterHelper.NormalizeEmploymentType(request.EmploymentType);
            query = normalizedEmploymentType switch
            {
                "FullTime" => query.Where(jp => jp.EmploymentType == "FullTime" || jp.EmploymentType == "Full-time"),
                "PartTime" => query.Where(jp => jp.EmploymentType == "PartTime" || jp.EmploymentType == "Part-time"),
                "Contract" => query.Where(jp => jp.EmploymentType == "Contract"),
                "Internship" => query.Where(jp => jp.EmploymentType == "Internship"),
                _ => query.Where(jp => jp.EmploymentType == request.EmploymentType)
            };
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(jp => jp.DepartmentId == request.DepartmentId.Value);
        }

        if (request.MinSalary.HasValue || request.MaxSalary.HasValue)
        {
            query = query.Where(jp => jp.ShowSalary);

            if (request.MinSalary.HasValue)
            {
                var minSalary = request.MinSalary.Value;
                query = query.Where(jp => (jp.SalaryRangeMax ?? jp.SalaryRangeMin ?? 0) >= minSalary);
            }

            if (request.MaxSalary.HasValue)
            {
                var maxSalary = request.MaxSalary.Value;
                query = query.Where(jp => (jp.SalaryRangeMin ?? jp.SalaryRangeMax ?? 0) <= maxSalary);
            }
        }

        var candidates = await query
            .Select(jp => new PublicJobPostingDto
            {
                Id = jp.Id,
                JobTitle = jp.JobTitle,
                JobCode = jp.JobCode,
                Description = jp.Description,
                Requirements = jp.Requirements,
                Benefits = jp.Benefits,
                EmploymentType = jp.EmploymentType,
                ExperienceLevel = jp.ExperienceLevel,
                EducationLevel = jp.EducationLevel,
                SalaryRangeMin = jp.ShowSalary ? jp.SalaryRangeMin : null,
                SalaryRangeMax = jp.ShowSalary ? jp.SalaryRangeMax : null,
                ShowSalary = jp.ShowSalary,
                Location = jp.Location,
                RemoteOption = jp.RemoteOption,
                Quantity = jp.Quantity,
                ApplicationDeadline = jp.ApplicationDeadline,
                PublishedAt = jp.PublishedAt,
                EnterpriseId = jp.EnterpriseId,
                EnterpriseName = jp.Enterprise.EnterpriseName,
                EnterpriseLogoUrl = jp.Enterprise.LogoUrl,
                DepartmentName = jp.Department.DepartmentName
            })
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.ExperienceBucket))
        {
            candidates = candidates
                .Where(job => PublicJobFilterHelper.MatchesExperienceBucket(job.ExperienceLevel, request.ExperienceBucket))
                .ToList();
        }

        candidates = ApplySort(candidates, request).ToList();

        var totalCount = candidates.Count;
        var safePageNumber = Math.Max(1, request.PageNumber);
        var safePageSize = Math.Max(1, request.PageSize);
        var items = candidates
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new GetPublicJobPostingsResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    private static IEnumerable<PublicJobPostingDto> ApplySort(
        IEnumerable<PublicJobPostingDto> candidates,
        GetPublicJobPostingsQuery request)
    {
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        var ordered = sortBy switch
        {
            "salary_desc" => candidates
                .OrderByDescending(job => job.ShowSalary ? (job.SalaryRangeMax ?? job.SalaryRangeMin ?? 0) : 0)
                .ThenByDescending(job => job.PublishedAt ?? DateTime.MinValue),
            "relevance" when !string.IsNullOrWhiteSpace(request.SearchTerm) => candidates
                .OrderByDescending(job => PublicJobFilterHelper.CalculateRelevanceScore(
                    job.JobTitle,
                    job.EnterpriseName,
                    job.Description,
                    request.SearchTerm))
                .ThenByDescending(job => job.PublishedAt ?? DateTime.MinValue),
            _ => candidates
                .OrderByDescending(job => job.PublishedAt ?? DateTime.MinValue)
        };

        return ordered.ThenByDescending(job => job.ApplicationDeadline ?? DateTime.MinValue);
    }
}
