using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Training.Queries.GetDepartmentTrainingSummary
{
    public sealed class GetDepartmentTrainingSummaryHandler
        : IRequestHandler<GetDepartmentTrainingSummaryQuery, GetDepartmentTrainingSummaryResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetDepartmentTrainingSummaryHandler> _logger;

        public GetDepartmentTrainingSummaryHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<GetDepartmentTrainingSummaryHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<GetDepartmentTrainingSummaryResult> Handle(
            GetDepartmentTrainingSummaryQuery request,
            CancellationToken cancellationToken)
        {
            var departmentId =
                await _currentUserService.GetDepartmentIdAsync();

            if (departmentId == null)
                throw new UnauthorizedAccessException();

            var enterpriseId =
                await _currentUserService.GetEnterpriseIdAsync();

            // 👥 Total Employees
            var totalEmployees = await _context.Employees
                .AsNoTracking()
                .CountAsync(e =>
                    e.DepartmentId == departmentId &&
                    !e.IsDeleted,
                    cancellationToken);

            // 📚 Total Courses
            var totalCourses = await _context.Courses
                .AsNoTracking()
                .CountAsync(c =>
                    c.EnterpriseId == enterpriseId &&
                    !c.IsDeleted,
                    cancellationToken);

            // 🎓 Studied Employees
            var studiedEmployees = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.Employee.DepartmentId == departmentId)
                .Select(e => e.EmployeeId)
                .Distinct()
                .CountAsync(cancellationToken);

            //   Passed Employees
            var passedEmployees = await _context.QuizAttempts
                .AsNoTracking()
                .Where(q =>
                    q.Enrollment.Employee.DepartmentId == departmentId &&
                    q.IsPassed == true)
                .Select(q => q.Enrollment.EmployeeId)
                .Distinct()
                .CountAsync(cancellationToken);

            // ❌ Failed Employees
            var failedEmployees = await _context.QuizAttempts
                .AsNoTracking()
                .Where(q =>
                    q.Enrollment.Employee.DepartmentId == departmentId &&
                    q.IsPassed == false)
                .Select(q => q.Enrollment.EmployeeId)
                .Distinct()
                .CountAsync(cancellationToken);

            var result = new DepartmentTrainingSummaryDto
            {
                DepartmentId = departmentId.Value,
                TotalEmployees = totalEmployees,
                TotalCourses = totalCourses,
                StudiedEmployees = studiedEmployees,
                PassedEmployees = passedEmployees,
                FailedEmployees = failedEmployees
            };

            return new GetDepartmentTrainingSummaryResult
            {
                Data = result
            };
        }
    }
}