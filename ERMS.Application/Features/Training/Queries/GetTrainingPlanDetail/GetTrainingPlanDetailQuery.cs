using MediatR;
using System;

namespace ERMS.Application.Features.Training.Queries.GetTrainingPlanDetail
{
    public sealed class GetTrainingPlanDetailQuery : IRequest<TrainingPlanDetailDto>
    {
        public Guid Id { get; set; }
    }
}