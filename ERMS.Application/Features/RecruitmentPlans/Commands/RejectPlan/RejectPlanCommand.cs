using MediatR;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.RejectPlan;

public sealed class RejectPlanCommand : IRequest<bool>
{
    public Guid PlanId { get; set; }
    public string RejectionReason { get; set; } = null!;
}
