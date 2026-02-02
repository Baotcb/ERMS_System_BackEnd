using MediatR;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.ApprovePlan;

public sealed class ApprovePlanCommand : IRequest<bool>
{
    public Guid PlanId { get; set; }
}
