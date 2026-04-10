using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Queries.GetTrainingRequestDetail
{
    public sealed class GetTrainingRequestDetailHandler
        : IRequestHandler<GetTrainingRequestDetailQuery, TrainingRequestDetailDto?>
    {
        private readonly IERMSDbContext _context;

        public GetTrainingRequestDetailHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<TrainingRequestDetailDto?> Handle(
            GetTrainingRequestDetailQuery request,
            CancellationToken cancellationToken)
        {
            return await _context.TrainingRequests
                .Where(t => t.Id == request.Id && !t.IsDeleted)
                .Select(t => new TrainingRequestDetailDto
                {
                    Id = t.Id,
                    EnterpriseId = t.EnterpriseId,
                    TrainingPlanId = t.TrainingPlanId,

                    DepartmentId = t.DepartmentId,
                    DepartmentName = t.Department.DepartmentName,

                    RequestedById = t.RequestedById,
                    RequestedByName = t.RequestedBy.FullName,

                    Subject = t.Subject,
                    Urgency = t.Urgency,
                    Status = t.Status,

                    Description = t.Description,
                    TargetAudience = t.TargetAudience,
                    EstimatedParticipants = t.EstimatedParticipants,
                    EstimatedBudget = t.EstimatedBudget,

                    ReviewNote = t.ReviewNote,

                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}