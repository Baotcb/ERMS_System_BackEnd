using System;
using System.Collections.Generic;
using MediatR;

namespace ERMS.Application.Features.Workshops.Commands.ConfirmWorkshopCompletion
{
    public sealed class ConfirmWorkshopCompletionCommand : IRequest<Guid>
    {
        public Guid CourseId { get; set; }
        public List<string> EvidencePhotoUrls { get; set; } = new();
        public string? Notes { get; set; }
    }
}
