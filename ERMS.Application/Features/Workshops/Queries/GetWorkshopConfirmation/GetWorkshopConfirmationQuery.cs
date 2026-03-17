using System;
using MediatR;

namespace ERMS.Application.Features.Workshops.Queries.GetWorkshopConfirmation
{
    public sealed class GetWorkshopConfirmationQuery : IRequest<GetWorkshopConfirmationResult?>
    {
        public Guid CourseId { get; set; }
    }

    public sealed class GetWorkshopConfirmationResult
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public Guid ConfirmedByUserId { get; set; }
        public string[] EvidencePhotoUrls { get; set; } = Array.Empty<string>();
        public string? Notes { get; set; }
        public DateTime ConfirmedAt { get; set; }
    }
}
