using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostingById;

/// <summary>
/// Handler to get a single published job posting by ID (public access)
/// </summary>
public sealed class GetPublicJobPostingByIdHandler : IRequestHandler<GetPublicJobPostingByIdQuery, PublicJobPostingDetailDto?>
{
    private readonly IERMSDbContext _context;

    public GetPublicJobPostingByIdHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<PublicJobPostingDetailDto?> Handle(GetPublicJobPostingByIdQuery request, CancellationToken cancellationToken)
    {
        var jobPosting = await _context.JobPostings
            .Include(jp => jp.Enterprise)
            .Include(jp => jp.Department)
            .Where(jp => jp.Id == request.Id
                      && jp.Status == JobPostingStatus.Published
                      && !jp.IsDeleted
                      && jp.Enterprise.Status == "Active"
                      && !jp.Enterprise.IsDeleted)
            .Select(jp => new PublicJobPostingDetailDto
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
                ViewCount = jp.ViewCount,
                EnterpriseId = jp.EnterpriseId,
                EnterpriseName = jp.Enterprise.EnterpriseName,
                EnterpriseLogoUrl = jp.Enterprise.LogoUrl,
                EnterpriseWebsite = jp.Enterprise.Website,
                DepartmentName = jp.Department.DepartmentName
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Increment view count if job was found
        if (jobPosting != null)
        {
            await _context.JobPostings
                .Where(jp => jp.Id == request.Id)
                .ExecuteUpdateAsync(setters => 
                    setters.SetProperty(jp => jp.ViewCount, jp => jp.ViewCount + 1), 
                    cancellationToken);
        }

        return jobPosting;
    }
}
