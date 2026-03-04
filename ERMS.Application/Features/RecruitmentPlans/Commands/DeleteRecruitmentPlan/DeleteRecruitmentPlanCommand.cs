using MediatR;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.DeleteRecruitmentPlan;

public sealed class DeleteRecruitmentPlanCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}
