using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Lessons.Queries.GetLessonProgress
{
    public sealed class GetLessonProgressByCourseHandler
        : IRequestHandler<GetLessonProgressByCourseQuery, List<LessonProgressDto>>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetLessonProgressByCourseHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<LessonProgressDto>> Handle(
            GetLessonProgressByCourseQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
                throw new UnauthorizedAccessException("Người dùng chưa được xác thực");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            if (employee == null)
                return new List<LessonProgressDto>();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.EmployeeId == employee.Id &&
                    e.CourseId == request.CourseId &&
                    !e.IsDeleted,
                    cancellationToken);

            if (enrollment == null)
                return new List<LessonProgressDto>();

            var lessons = await _context.Lessons
                .Where(l => l.CourseId == request.CourseId && !l.IsDeleted)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new LessonProgressDto
                {
                    LessonId = l.Id,
                    LessonTitle = l.LessonTitle,
                    OrderIndex = l.OrderIndex,

                    WatchPercentage = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == enrollment.Id && p.LessonId == l.Id)
                        .Select(p => p.WatchPercentage)
                        .FirstOrDefault(),

                    LastPosition = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == enrollment.Id && p.LessonId == l.Id)
                        .Select(p => p.LastPosition)
                        .FirstOrDefault(),

                    TimeSpentMinutes = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == enrollment.Id && p.LessonId == l.Id)
                        .Select(p => p.TimeSpentMinutes)
                        .FirstOrDefault(),

                    Status = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == enrollment.Id && p.LessonId == l.Id)
                        .Select(p => p.Status)
                        .FirstOrDefault() ?? "NotStarted",

                    StartedAt = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == enrollment.Id && p.LessonId == l.Id)
                        .Select(p => p.StartedAt)
                        .FirstOrDefault(),

                    CompletedAt = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == enrollment.Id && p.LessonId == l.Id)
                        .Select(p => p.CompletedAt)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return lessons;
        }
    }
}
