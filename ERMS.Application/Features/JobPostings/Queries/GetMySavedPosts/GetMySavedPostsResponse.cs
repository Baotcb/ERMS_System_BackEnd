namespace ERMS.Application.Features.JobPostings.Queries.GetMySavedPosts;

/// <summary>
/// Response containing the candidate's saved job postings with pagination
/// </summary>
public sealed class GetMySavedPostsResponse
{
    public List<SavedPostDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public sealed class SavedPostDto
{
    public Guid SavedJobId { get; set; }
    public Guid JobPostingId { get; set; }
    public string JobTitle { get; set; } = null!;
    public string? JobCode { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string EmploymentType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime SavedAt { get; set; }
}
