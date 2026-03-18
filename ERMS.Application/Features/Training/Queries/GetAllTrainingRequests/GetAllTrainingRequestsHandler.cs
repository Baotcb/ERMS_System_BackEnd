using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Queries.GetAllTrainingRequests
{
    public sealed class GetAllTrainingRequestsHandler
        : IRequestHandler<GetAllTrainingRequestsQuery, GetAllTrainingRequestsResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAllTrainingRequestsHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetAllTrainingRequestsResult> Handle(
            GetAllTrainingRequestsQuery request,
            CancellationToken cancellationToken)
        {
           

            var query = _context.TrainingRequests
                .Where(t =>  !t.IsDeleted)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(t =>
                    t.Subject.ToLower().Contains(search) ||
                    t.RequestedBy.FullName.ToLower().Contains(search));
            }

            // Department
            if (request.DepartmentId.HasValue)
            {
                query = query.Where(t => t.DepartmentId == request.DepartmentId.Value);
            }

            // Status
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(t => t.Status == request.Status);
            }

            // Urgency
            if (!string.IsNullOrEmpty(request.Urgency))
            {
                query = query.Where(t => t.Urgency == request.Urgency);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TrainingRequestDto
                {
                    Id = t.Id,
                    Subject = t.Subject,
                    Urgency = t.Urgency,
                    Status = t.Status,
                    DepartmentName = t.Department.DepartmentName,
                    RequestedByName = t.RequestedBy.FullName,
                    TargetAudience = t.TargetAudience,
                    EstimatedParticipants = t.EstimatedParticipants,
                    EstimatedBudget = t.EstimatedBudget,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new GetAllTrainingRequestsResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}