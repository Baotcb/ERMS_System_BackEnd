using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Feedback.Queries.GetCourseFeedbacks
{
    public class GetCourseFeedbacksQuery : IRequest<List<CourseFeedbackDto>> { }

    public class CourseFeedbackDto
    {
        public string Id { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string CourseCode { get; set; } = "";
        public string TrainerEmail { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string EmployeeEmail { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public int CourseRating { get; set; }
        public int TrainerRating { get; set; }
        public string? Comment { get; set; }
        public bool IsAnonymous { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetCourseFeedbacksHandler
        : IRequestHandler<GetCourseFeedbacksQuery, List<CourseFeedbackDto>>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetCourseFeedbacksHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<CourseFeedbackDto>> Handle(
            GetCourseFeedbacksQuery request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) return new List<CourseFeedbackDto>();

            var feedbacks = await _context.CourseFeedbacks
                .Include(f => f.Course)
                .Include(f => f.Employee).ThenInclude(e => e.User)
                .Include(f => f.Employee).ThenInclude(e => e.Department)
                .Where(f => f.Course.EnterpriseId == enterpriseId)
                .OrderByDescending(f => f.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return feedbacks.Select(f => new CourseFeedbackDto
            {
                Id = f.Id.ToString(),
                CourseName = f.Course?.CourseName ?? "N/A",
                CourseCode = f.Course?.CourseCode ?? "",
                TrainerEmail = f.Course?.TrainerEmail ?? "",
                EmployeeName = f.IsAnonymous ? "Ẩn danh" : (f.Employee?.User?.FullName ?? f.Employee?.User?.Email ?? "N/A"),
                EmployeeEmail = f.IsAnonymous ? "" : (f.Employee?.User?.Email ?? ""),
                DepartmentName = f.IsAnonymous ? "" : (f.Employee?.Department?.DepartmentName ?? ""),
                CourseRating = f.CourseRating,
                TrainerRating = f.TrainerRating,
                Comment = f.Comment,
                IsAnonymous = f.IsAnonymous,
                CreatedAt = f.CreatedAt
            }).ToList();
        }
    }
}
