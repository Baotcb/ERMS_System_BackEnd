using MediatR;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.SubmitPlan;

public sealed class SubmitPlanCommand : IRequest<bool>
{
    public Guid PlanId { get; set; }
}
