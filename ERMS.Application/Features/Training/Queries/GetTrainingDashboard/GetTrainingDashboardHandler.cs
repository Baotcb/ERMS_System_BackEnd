using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Dashboard.Queries.GetTrainingDashboard
{
    public sealed class GetTrainingDashboardHandler
        : IRequestHandler<GetTrainingDashboardQuery, GetTrainingDashboardResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetTrainingDashboardHandler> _logger;

        public GetTrainingDashboardHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<GetTrainingDashboardHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<GetTrainingDashboardResult> Handle(
            GetTrainingDashboardQuery request,
            CancellationToken cancellationToken)
        {
            //  CHỈ DIRECTOR XEM ĐƯỢC
            var roles = _currentUserService.Roles;

            if (!roles.Contains(AppRoles.Director))
                throw new UnauthorizedAccessException("Chỉ Director mới được xem dashboard");

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            // ===== TRAINING PLAN =====
            var plans = _context.TrainingPlans
                .Where(x => !x.IsDeleted && x.EnterpriseId == enterpriseId);

            var totalPlans = await plans.CountAsync(cancellationToken);

            var pendingPlans = await plans.CountAsync(x =>
                x.Status == "Pending" || x.Status == "Draft", cancellationToken);

            var approvedPlans = await plans.CountAsync(x =>
                x.Status == "Approved", cancellationToken);

            var approvedBudget = await plans
                .Where(x => x.Status == "Approved")
                .SumAsync(x => x.TotalBudget ?? 0, cancellationToken);

            // ===== COURSE =====
            var courses = _context.Courses
                .Where(x => !x.IsDeleted && x.EnterpriseId == enterpriseId);

            var totalCourses = await courses.CountAsync(cancellationToken);

            var activeCourses = await courses.CountAsync(x =>
                x.Status == "Published" || x.Status == "Ongoing",
                cancellationToken);

            var coursesWithContent = await courses
                .CountAsync(x => x.Lessons.Any(l => !l.IsDeleted), cancellationToken);

            var readinessPercent = totalCourses == 0
                ? 0
                : (double)coursesWithContent / totalCourses * 100;

            // ===== ENROLLMENT =====
            var enrollments = _context.Enrollments
                .Where(x => !x.IsDeleted);

            var totalEnrollments = await enrollments.CountAsync(cancellationToken);

            var avgStudentsPerCourse = totalCourses == 0
                ? 0
                : (double)totalEnrollments / totalCourses;

            // ===== RESULT =====
            var dto = new TrainingDashboardDto
            {
                TotalPlans = totalPlans,
                PendingPlans = pendingPlans,
                ApprovedPlans = approvedPlans,
                ApprovedBudget = approvedBudget,

                ActiveCourses = activeCourses,
                TotalCourses = totalCourses,
                CoursesWithContent = coursesWithContent,
                ReadinessPercent = Math.Round(readinessPercent, 2),

                TotalEnrollments = totalEnrollments,
                AvgStudentsPerCourse = Math.Round(avgStudentsPerCourse, 2)
            };

            return new GetTrainingDashboardResult
            {
                Data = dto
            };
        }
    }
}