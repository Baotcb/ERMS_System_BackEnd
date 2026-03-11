using ERMS.Application.Features.JobPostings.Queries.PublicJobFiltering;

namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobFilterOptions;

public sealed class GetPublicJobFilterOptionsResponse
{
    public List<PublicDepartmentFilterDto> Departments { get; set; } = [];
    public List<string> Locations { get; set; } = [];
    public List<PublicJobFilterOption> EmploymentTypes { get; set; } = [];
    public List<PublicJobFilterOption> ExperienceBuckets { get; set; } = [];
    public List<PublicSalaryBucket> SalaryBuckets { get; set; } = [];
}

public sealed class PublicDepartmentFilterDto
{
    public int Id { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int JobCount { get; set; }
}
