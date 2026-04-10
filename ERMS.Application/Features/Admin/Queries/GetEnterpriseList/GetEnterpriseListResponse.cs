namespace ERMS.Application.Features.Admin.Queries.GetEnterpriseList;

public sealed class GetEnterpriseListResponse
{
    public List<EnterpriseListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public sealed class EnterpriseListItemDto
{
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string EnterpriseCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? CurrentPlanName { get; set; }
    public string? CurrentPlanCode { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EmployeeCount { get; set; }
    public string? LogoUrl { get; set; }
}
