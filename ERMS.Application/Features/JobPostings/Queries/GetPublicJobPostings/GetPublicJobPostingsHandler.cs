using ERMS.Application.Interface;
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
        // Only show Published job postings that are not deleted and not past deadline
        var query = _context.JobPostings
            .Include(jp => jp.Enterprise)
            .Include(jp => jp.Department)
            .Where(jp => jp.Status == JobPostingStatus.Published
                      && !jp.IsDeleted
                      && jp.Enterprise.Status == "Active"
                      && !jp.Enterprise.IsDeleted);

        // Filter out expired jobs (past application deadline)
        query = query.Where(jp => !jp.ApplicationDeadline.HasValue 
                               || jp.ApplicationDeadline.Value >= DateTime.UtcNow);

        // Optional filters
        if (request.EnterpriseId.HasValue)
        {
            query = query.Where(jp => jp.EnterpriseId == request.EnterpriseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim().ToLower();
            query = query.Where(jp => jp.JobTitle.ToLower().Contains(searchTerm)
                                   || (jp.Description != null && jp.Description.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var location = request.Location.Trim().ToLower();
            query = query.Where(jp => jp.Location != null && jp.Location.ToLower().Contains(location));
        }

        if (!string.IsNullOrWhiteSpace(request.EmploymentType))
        {
            query = query.Where(jp => jp.EmploymentType == request.EmploymentType);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(jp => jp.PublishedAt)
            .ThenByDescending(jp => jp.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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
                EnterpriseName = jp.Enterprise.EnterpriseName,
                EnterpriseLogoUrl = jp.Enterprise.LogoUrl,
                DepartmentName = jp.Department.DepartmentName
            })
            .ToListAsync(cancellationToken);

        return new GetPublicJobPostingsResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
