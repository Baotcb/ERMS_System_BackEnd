namespace ERMS.Application.Features.JobPostings.Queries.GetJobPostings;

public sealed class GetJobPostingsResponse
{
    public List<JobPostingListDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public sealed class JobPostingListDto
{
    public Guid Id { get; set; }
    public Guid? PlanDetailId { get; set; }
    public string JobTitle { get; set; } = null!;
    public string? JobCode { get; set; }
    public string Status { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;
    public string? Location { get; set; }
    public int Quantity { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ApplicationCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
