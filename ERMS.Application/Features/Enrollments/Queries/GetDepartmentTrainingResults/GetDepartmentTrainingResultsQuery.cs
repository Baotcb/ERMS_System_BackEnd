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
        public DateTime AssignedAt { get; set; }
        public int ProgressPercentage { get; set; }
        public double? QuizScore { get; set; }
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

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Employee).ThenInclude(emp => emp.User)
                .Include(e => e.Employee).ThenInclude(emp => emp.Department)
                .Include(e => e.QuizAttempts)
                .Where(e => !e.IsDeleted && e.Course.EnterpriseId == enterpriseId)
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

                return new DepartmentTrainingResultDto
                {
                    Id = e.Id.ToString(),
                    EmployeeName = e.Employee?.User?.FullName ?? e.Employee?.User?.Email ?? "N/A",
                    EmployeeEmail = e.Employee?.User?.Email ?? "",
                    DepartmentName = e.Employee?.Department?.DepartmentName ?? "N/A",
                    CourseName = e.Course?.CourseName ?? "N/A",
                    AssignedAt = e.EnrolledAt,
                    ProgressPercentage = e.Progress,
                    QuizScore = latestAttempt != null ? (double?)latestAttempt.Score : null,
                    LearningStatus = e.Status,
                    EvaluationStatus = hasPassed ? "Passed" : (hasFailed ? "Failed" : "Pending"),
                    Note = e.Note
                };
            }).ToList();
        }
    }
}
