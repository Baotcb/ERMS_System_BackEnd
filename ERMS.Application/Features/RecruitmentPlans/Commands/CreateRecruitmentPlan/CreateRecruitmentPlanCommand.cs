using MediatR;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.CreateRecruitmentPlan;

public sealed class CreateRecruitmentPlanCommand : IRequest<Guid>
{
    public Guid CampaignId { get; set; }
    public int DepartmentId { get; set; }
    public string PlanName { get; set; } = null!;
    public string PlanCode { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? TotalBudget { get; set; }
}
