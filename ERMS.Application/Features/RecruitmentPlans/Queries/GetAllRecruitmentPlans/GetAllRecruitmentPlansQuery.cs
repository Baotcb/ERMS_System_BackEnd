using MediatR;

namespace ERMS.Application.Features.RecruitmentPlans.Queries.GetAllRecruitmentPlans;

public sealed class GetAllRecruitmentPlansQuery : IRequest<GetAllRecruitmentPlansResult>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? CampaignId { get; set; }
}

public sealed class GetAllRecruitmentPlansResult
{
    public List<RecruitmentPlanDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public sealed class RecruitmentPlanDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public string PlanName { get; set; } = null!;
    public string PlanCode { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? TotalBudget { get; set; }
    public string Status { get; set; } = null!;
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? CreatedByName { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
