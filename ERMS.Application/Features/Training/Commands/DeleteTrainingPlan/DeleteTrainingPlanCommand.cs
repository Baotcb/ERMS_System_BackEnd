using MediatR;

namespace ERMS.Application.Features.Training.Commands.DeleteTrainingPlan
{
    public sealed class DeleteTrainingPlanCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
    }
}