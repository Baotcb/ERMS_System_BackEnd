using MediatR;

namespace ERMS.Application.Features.RecruitmentCampaigns.Commands.CreateRecruitmentCampaign;

public sealed class CreateRecruitmentCampaignCommand : IRequest<Guid>
{
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
}
