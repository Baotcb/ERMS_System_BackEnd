using ERMS.Application.Features.Lessons.Queries.GetLessonProgress;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Lessons.Queries.GetLessonProgress
{
    public sealed class GetLessonProgressByEnrollmentHandler
        : IRequestHandler<GetLessonProgressByEnrollmentQuery, List<LessonProgressDto>>
    {
        private readonly IERMSDbContext _context;

        public GetLessonProgressByEnrollmentHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<List<LessonProgressDto>> Handle(
            GetLessonProgressByEnrollmentQuery request,
            CancellationToken cancellationToken)
        {
            var lessons = await _context.Lessons
                .Where(l => !l.IsDeleted)
                .Where(l => _context.Enrollments
                    .Any(e => e.Id == request.EnrollmentId && e.CourseId == l.CourseId))
                .OrderBy(l => l.OrderIndex)
                .Select(l => new LessonProgressDto
                {
                    LessonId = l.Id,
                    LessonTitle = l.LessonTitle,
                    OrderIndex = l.OrderIndex,

                    WatchPercentage = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == request.EnrollmentId && p.LessonId == l.Id)
                        .Select(p => p.WatchPercentage)
                        .FirstOrDefault(),

                    LastPosition = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == request.EnrollmentId && p.LessonId == l.Id)
                        .Select(p => p.LastPosition)
                        .FirstOrDefault(),

                    TimeSpentMinutes = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == request.EnrollmentId && p.LessonId == l.Id)
                        .Select(p => p.TimeSpentMinutes)
                        .FirstOrDefault(),

                    Status = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == request.EnrollmentId && p.LessonId == l.Id)
                        .Select(p => p.Status)
                        .FirstOrDefault() ?? "NotStarted",

                    StartedAt = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == request.EnrollmentId && p.LessonId == l.Id)
                        .Select(p => p.StartedAt)
                        .FirstOrDefault(),

                    CompletedAt = _context.LessonProgresses
                        .Where(p => p.EnrollmentId == request.EnrollmentId && p.LessonId == l.Id)
                        .Select(p => p.CompletedAt)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return lessons;
        }
    }
}