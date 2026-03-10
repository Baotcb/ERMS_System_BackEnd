using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enrollments.Commands.AssignEmployeesToCourse
{
    public sealed class AssignEmployeesToCourseHandler
        : IRequestHandler<AssignEmployeesToCourseCommand, AssignEmployeesToCourseResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AssignEmployeesToCourseHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssignEmployeesToCourseResult> Handle(
            AssignEmployeesToCourseCommand request,
            CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.Id == request.CourseId &&
                    c.EnterpriseId == enterpriseId.Value &&
                    !c.IsDeleted,
                    cancellationToken);

            if (course == null)
                throw new KeyNotFoundException("Không tìm thấy khóa học");

            var existingEnrollments = await _context.Enrollments
                .Where(e => e.CourseId == request.CourseId && !e.IsDeleted)
                .Select(e => e.EmployeeId)
                .ToListAsync(cancellationToken);

            var assigned = new List<Guid>();
            var skipped = new List<Guid>();

            foreach (var employeeId in request.EmployeeIds.Distinct())
            {
                if (existingEnrollments.Contains(employeeId))
                {
                    skipped.Add(employeeId);
                    continue;
                }

                var enrollment = new Enrollment
                {
                    Id = Guid.NewGuid(),
                    CourseId = request.CourseId,
                    EmployeeId = employeeId,
                    EnrolledAt = DateTime.UtcNow,
                    Status = "NotStarted",
                    Progress = 0
                };

                _context.Enrollments.Add(enrollment);

                assigned.Add(employeeId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new AssignEmployeesToCourseResult
            {
                TotalAssigned = assigned.Count,
                AssignedEmployeeIds = assigned,
                SkippedEmployeeIds = skipped
            };
        }
    }
}