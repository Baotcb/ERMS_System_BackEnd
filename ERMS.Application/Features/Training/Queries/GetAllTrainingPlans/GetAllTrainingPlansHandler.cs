using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Training.Queries.GetAllTrainingPlans
{
    public sealed class GetAllTrainingPlansHandler
        : IRequestHandler<GetAllTrainingPlansQuery, GetAllTrainingPlansResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetAllTrainingPlansHandler> _logger;

        public GetAllTrainingPlansHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<GetAllTrainingPlansHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<GetAllTrainingPlansResult> Handle(
            GetAllTrainingPlansQuery request,
            CancellationToken cancellationToken)
        {
            var enterpriseId =
                await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new UnauthorizedAccessException();

            var query = _context.TrainingPlans
                .AsNoTracking()
                .Where(p =>
                    p.EnterpriseId == enterpriseId &&
                    !p.IsDeleted)
                .Include(p => p.CreatedBy)
                .AsQueryable();

            // 🔎 Search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(p =>
                    p.PlanName.Contains(request.Search) ||
                    p.PlanCode.Contains(request.Search));
            }

            // 📌 Filter Status
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(p =>
                    p.Status == request.Status);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new TrainingPlanDto
                {
                    Id = p.Id,
                    PlanName = p.PlanName,
                    PlanCode = p.PlanCode,
                    Description = p.Description,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    TotalBudget = p.TotalBudget,
                    Status = p.Status,
                    CreatedBy = p.CreatedBy.FullName,
                    CreatedAt = p.CreatedAt,
                    ReviewNote = p.ReviewNote,
                    Year = p.StartDate.Year,
                    TotalCourses = p.Courses.Count
                })
                .ToListAsync(cancellationToken);

            return new GetAllTrainingPlansResult
            {
                TotalCount = totalCount,
                Items = items
            };
        }
    }
}