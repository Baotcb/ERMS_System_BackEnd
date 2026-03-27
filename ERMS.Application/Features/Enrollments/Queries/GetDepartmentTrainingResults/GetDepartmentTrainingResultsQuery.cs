using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Enrollments.Queries.GetDepartmentTrainingResults
{
    public class GetDepartmentTrainingResultsQuery : IRequest<List<DepartmentTrainingResultDto>> { }

    public class DepartmentTrainingResultDto
    {
        public string Id { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string EmployeeEmail { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string CourseCode { get; set; } = "";
        public DateTime AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int ProgressPercentage { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public double? QuizScore { get; set; }
        public int AttemptCount { get; set; }
        public string LearningStatus { get; set; } = "NotStarted";
        public string EvaluationStatus { get; set; } = "Pending";
        public string? Note { get; set; }
    }

    public class GetDepartmentTrainingResultsHandler
        : IRequestHandler<GetDepartmentTrainingResultsQuery, List<DepartmentTrainingResultDto>>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetDepartmentTrainingResultsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<DepartmentTrainingResultDto>> Handle(
            GetDepartmentTrainingResultsQuery request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null) return new List<DepartmentTrainingResultDto>();

            var departmentId = await _currentUserService.GetDepartmentIdAsync();

            var query = _context.Enrollments
                .Include(e => e.Course).ThenInclude(c => c.Lessons)
                .Include(e => e.Employee).ThenInclude(emp => emp.User)
                .Include(e => e.Employee).ThenInclude(emp => emp.Department)
                .Include(e => e.QuizAttempts)
                .Include(e => e.LessonProgresses)
                .Where(e => !e.IsDeleted && e.Course.EnterpriseId == enterpriseId);

            // Dept-Head scoping: only show own department's results
            if (departmentId.HasValue)
            {
                query = query.Where(e => e.Employee.DepartmentId == departmentId.Value);
            }

            var enrollments = await query
                .OrderByDescending(e => e.EnrolledAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return enrollments.Select(e =>
            {
                var latestAttempt = e.QuizAttempts?
                    .OrderByDescending(q => q.CompletedAt)
                    .FirstOrDefault();

                var hasPassed = e.QuizAttempts?.Any(q => q.IsPassed == true) ?? false;
                var hasFailed = e.QuizAttempts?.Any(q => q.IsPassed == false) ?? false;
                var attempts = e.QuizAttempts?.Count ?? 0;

                // Count from actual LessonProgress records (not stale Enrollment.Progress)
                var totalLessons = e.Course?.Lessons?.Count(l => !l.IsDeleted) ?? 0;
                var completedLessons = e.LessonProgresses?.Count(lp => lp.Status == "Completed") ?? 0;
                var progressPct = totalLessons > 0
                    ? (int)Math.Round(((double)completedLessons / totalLessons) * 100)
                    : 0;

                return new DepartmentTrainingResultDto
                {
                    Id = e.Id.ToString(),
                    EmployeeName = e.Employee?.User?.FullName ?? e.Employee?.User?.Email ?? "N/A",
                    EmployeeEmail = e.Employee?.User?.Email ?? "",
                    DepartmentName = e.Employee?.Department?.DepartmentName ?? "N/A",
                    CourseName = e.Course?.CourseName ?? "N/A",
                    CourseCode = e.Course?.CourseCode ?? "",
                    AssignedAt = e.EnrolledAt,
                    CompletedAt = latestAttempt?.CompletedAt,
                    ProgressPercentage = progressPct,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    QuizScore = latestAttempt != null ? (double?)latestAttempt.Score : null,
                    AttemptCount = attempts,
                    LearningStatus = e.Status,
                    EvaluationStatus = hasPassed ? "Passed" : (hasFailed ? "Failed" : "Pending"),
                    Note = e.Note
                };
            }).ToList();
        }
    }
}
