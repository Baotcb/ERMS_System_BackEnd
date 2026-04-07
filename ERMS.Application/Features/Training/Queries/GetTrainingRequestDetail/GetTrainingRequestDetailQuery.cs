using MediatR;
using System;

namespace ERMS.Application.Features.Training.Queries.GetTrainingRequestDetail
{
    public sealed class GetTrainingRequestDetailQuery
        : IRequest<TrainingRequestDetailDto?>
    {
        public Guid Id { get; set; }
    }
}