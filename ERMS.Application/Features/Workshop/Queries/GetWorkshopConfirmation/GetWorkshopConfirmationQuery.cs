using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Workshop.Queries.GetWorkshopConfirmation
{
    public class GetWorkshopConfirmationQuery : IRequest<WorkshopConfirmationDto?>
    {
        public Guid CourseId { get; set; }
    }

    public class WorkshopConfirmationDto
    {
        public string Id { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string ConfirmedByUserId { get; set; } = "";
        public List<string> EvidencePhotoUrls { get; set; } = new();
        public string? Notes { get; set; }
        public DateTime ConfirmedAt { get; set; }
    }

    public class GetWorkshopConfirmationHandler
        : IRequestHandler<GetWorkshopConfirmationQuery, WorkshopConfirmationDto?>
    {
        private readonly IERMSDbContext _context;

        public GetWorkshopConfirmationHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<WorkshopConfirmationDto?> Handle(
            GetWorkshopConfirmationQuery request, CancellationToken cancellationToken)
        {
            var confirmation = await _context.WorkshopConfirmations
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.CourseId == request.CourseId, cancellationToken);

            if (confirmation == null) return null;

            return new WorkshopConfirmationDto
            {
                Id = confirmation.Id.ToString(),
                CourseId = confirmation.CourseId.ToString(),
                ConfirmedByUserId = confirmation.ConfirmedByUserId.ToString(),
                EvidencePhotoUrls = confirmation.EvidencePhotoUrls,
                Notes = confirmation.Notes,
                ConfirmedAt = confirmation.ConfirmedAt
            };
        }
    }
}
