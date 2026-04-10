using MediatR;
using System;

namespace ERMS.Application.Features.Training.Commands.DeleteTrainingRequest
{
    public sealed class DeleteTrainingRequestCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}