using MediatR;

namespace ERMS.Application.Features.PlanDetails.Commands.DeletePlanDetail;

public sealed class DeletePlanDetailCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
