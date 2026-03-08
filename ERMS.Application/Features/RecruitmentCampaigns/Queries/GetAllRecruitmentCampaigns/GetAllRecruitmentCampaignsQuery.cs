using MediatR;

namespace ERMS.Application.Features.RecruitmentCampaigns.Queries.GetAllRecruitmentCampaigns;

public sealed class GetAllRecruitmentCampaignsQuery : IRequest<GetAllRecruitmentCampaignsResult>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int? FiscalYear { get; set; }
    public byte? FiscalQuarter { get; set; }
}

public sealed class GetAllRecruitmentCampaignsResult
{
    public List<RecruitmentCampaignDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public sealed class RecruitmentCampaignDto
{
    public Guid Id { get; set; }
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
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int TotalPlansCount { get; set; }
    public decimal ActualCost { get; set; }
}
