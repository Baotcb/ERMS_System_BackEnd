using System.Linq.Expressions;
using ERMS.Application.Features.JobPostings.Queries.PublicJobFiltering;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Entities.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostings;

/// <summary>
/// Handler for public job postings query (no authentication required)
/// </summary>
public sealed class GetPublicJobPostingsHandler : IRequestHandler<GetPublicJobPostingsQuery, GetPublicJobPostingsResponse>
{
    private static readonly Expression<Func<JobPosting, PublicJobPostingDto>> PublicJobPostingProjection = jp => new PublicJobPostingDto
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
    };

    private readonly IERMSDbContext _context;

    public GetPublicJobPostingsHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetPublicJobPostingsResponse> Handle(GetPublicJobPostingsQuery request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var safePageNumber = Math.Max(1, request.PageNumber);
        var safePageSize = Math.Max(1, request.PageSize);
        var skip = (safePageNumber - 1) * safePageSize;

        IQueryable<JobPosting> query = _context.JobPostings
            .AsNoTracking()
            .Where(jp => jp.Status == JobPostingStatus.Published
                      && !jp.IsDeleted
                      && jp.Enterprise.Status == "Active"
                      && !jp.Enterprise.IsDeleted)
            .Where(jp => !jp.ApplicationDeadline.HasValue || jp.ApplicationDeadline.Value >= utcNow);

        if (request.EnterpriseId.HasValue)
        {
            query = query.Where(jp => jp.EnterpriseId == request.EnterpriseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(jp => jp.JobTitle.ToLower().Contains(searchTerm)
                                   || jp.Enterprise.EnterpriseName.ToLower().Contains(searchTerm)
                                   || (jp.Description != null && jp.Description.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var location = request.Location.Trim().ToLowerInvariant();
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

        var orderedQuery = ApplySort(query, request);

        List<PublicJobPostingDto> items;
        int totalCount;

        if (string.IsNullOrWhiteSpace(request.ExperienceBucket))
        {
            totalCount = await orderedQuery.CountAsync(cancellationToken);
            items = await orderedQuery
                .Skip(skip)
                .Take(safePageSize)
                .Select(PublicJobPostingProjection)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var matchingIds = (await orderedQuery
                    .Select(jp => new ExperienceFilterCandidate(jp.Id, jp.ExperienceLevel))
                    .ToListAsync(cancellationToken))
                .Where(candidate => PublicJobFilterHelper.MatchesExperienceBucket(candidate.ExperienceLevel, request.ExperienceBucket))
                .Select(candidate => candidate.Id)
                .ToList();

            totalCount = matchingIds.Count;

            var pageIds = matchingIds
                .Skip(skip)
                .Take(safePageSize)
                .ToList();

            items = pageIds.Count == 0
                ? []
                : await _context.JobPostings
                    .AsNoTracking()
                    .Where(jp => pageIds.Contains(jp.Id))
                    .Select(PublicJobPostingProjection)
                    .ToListAsync(cancellationToken);

            if (items.Count > 1)
            {
                var itemOrder = pageIds
                    .Select((id, index) => new { id, index })
                    .ToDictionary(entry => entry.id, entry => entry.index);

                items = items
                    .OrderBy(item => itemOrder[item.Id])
                    .ToList();
            }
        }

        return new GetPublicJobPostingsResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    private static IOrderedQueryable<JobPosting> ApplySort(
        IQueryable<JobPosting> query,
        GetPublicJobPostingsQuery request)
    {
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        var normalizedSearchTerm = request.SearchTerm?.Trim().ToLowerInvariant();

        var ordered = sortBy switch
        {
            "salary_desc" => query
                .OrderByDescending(job => job.ShowSalary ? (job.SalaryRangeMax ?? job.SalaryRangeMin ?? 0) : 0)
                .ThenByDescending(job => job.PublishedAt ?? DateTime.MinValue),
            "relevance" when !string.IsNullOrWhiteSpace(normalizedSearchTerm) => query
                .OrderByDescending(job =>
                    job.JobTitle.ToLower().Contains(normalizedSearchTerm!)
                        ? 3
                        : job.Enterprise.EnterpriseName.ToLower().Contains(normalizedSearchTerm!)
                            ? 2
                            : job.Description != null && job.Description.ToLower().Contains(normalizedSearchTerm!)
                                ? 1
                                : 0)
                .ThenByDescending(job => job.PublishedAt ?? DateTime.MinValue),
            _ => query
                .OrderByDescending(job => job.PublishedAt ?? DateTime.MinValue)
        };

        return ordered.ThenByDescending(job => job.ApplicationDeadline ?? DateTime.MinValue);
    }

    private sealed record ExperienceFilterCandidate(Guid Id, string? ExperienceLevel);
}
