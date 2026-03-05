using MediatR;

namespace ERMS.Application.Features.RecruitmentCampaigns.Queries.GetRecruitmentCampaignById;

public sealed class GetRecruitmentCampaignByIdQuery : IRequest<RecruitmentCampaignDetailDto>
{
    public Guid Id { get; set; }
}

public sealed class RecruitmentCampaignDetailDto
{
    public Guid Id { get; set; }
    public Guid EnterpriseId { get; set; }
    public string CampaignName { get; set; } = null!;
    public string CampaignCode { get; set; } = null!;
    public string? Description { get; set; }
    public int FiscalYear { get; set; }
    public byte? FiscalQuarter { get; set; }
    public DateTime SubmissionStartDate { get; set; }
    public DateTime SubmissionEndDate { get; set; }
    public DateTime? TargetHireStartDate { get; set; }
    public DateTime? TargetHireEndDate { get; set; }
    public decimal? TotalBudgetCeiling { get; set; }
    public int? MaxTotalPositions { get; set; }
    public string Status { get; set; } = null!;
    public Guid CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int TotalPlansCount { get; set; }
    public decimal UsedBudget { get; set; }
    public decimal PendingBudget { get; set; }
    public decimal RemainingBudget { get; set; }
}
