using MediatR;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.UpdateRecruitmentPlan;

public sealed class UpdateRecruitmentPlanCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = null!;
    public string PlanCode { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? TotalBudget { get; set; }
}
