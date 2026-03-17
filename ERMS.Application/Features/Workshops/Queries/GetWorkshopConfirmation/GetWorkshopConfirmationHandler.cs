using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Workshops.Queries.GetWorkshopConfirmation
{
    public sealed class GetWorkshopConfirmationHandler
        : IRequestHandler<GetWorkshopConfirmationQuery, GetWorkshopConfirmationResult?>
    {
        private readonly IERMSDbContext _context;

        public GetWorkshopConfirmationHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<GetWorkshopConfirmationResult?> Handle(
            GetWorkshopConfirmationQuery request,
            CancellationToken cancellationToken)
        {
            var confirmation = await _context.WorkshopConfirmations
                .FirstOrDefaultAsync(w => w.CourseId == request.CourseId, cancellationToken);

            if (confirmation == null)
            {
                return null;
            }

            string[] photoUrls;
            try
            {
                photoUrls = JsonSerializer.Deserialize<string[]>(confirmation.EvidencePhotoUrls) 
                    ?? System.Array.Empty<string>();
            }
            catch
            {
                photoUrls = System.Array.Empty<string>();
            }

            return new GetWorkshopConfirmationResult
            {
                Id = confirmation.Id,
                CourseId = confirmation.CourseId,
                ConfirmedByUserId = confirmation.ConfirmedByUserId,
                EvidencePhotoUrls = photoUrls,
                Notes = confirmation.Notes,
                ConfirmedAt = confirmation.ConfirmedAt
            };
        }
    }
}
