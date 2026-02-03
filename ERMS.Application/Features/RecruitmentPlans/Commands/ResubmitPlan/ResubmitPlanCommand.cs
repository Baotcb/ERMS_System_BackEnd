using MediatR;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.ResubmitPlan;

public sealed class ResubmitPlanCommand : IRequest<bool>
{
    public Guid PlanId { get; set; }
}
