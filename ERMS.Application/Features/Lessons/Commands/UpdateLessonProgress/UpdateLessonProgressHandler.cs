using ERMS.Application.Features.Lessons.Commands.UpdateLessonProgress;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Lessons.Commands.UpdateLessonProgress
{
    public sealed class UpdateLessonProgressHandler
        : IRequestHandler<UpdateLessonProgressCommand, bool>
    {
        private readonly IERMSDbContext _context;

        public UpdateLessonProgressHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateLessonProgressCommand request, CancellationToken cancellationToken)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId && !e.IsDeleted, cancellationToken);

            if (enrollment == null)
                throw new KeyNotFoundException("Enrollment not found");

            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(p =>
                    p.EnrollmentId == request.EnrollmentId &&
                    p.LessonId == request.LessonId,
                    cancellationToken);

            if (progress == null)
            {
                progress = new LessonProgress
                {
                    Id = Guid.NewGuid(),
                    EnrollmentId = request.EnrollmentId,
                    LessonId = request.LessonId,
                    StartedAt = DateTime.UtcNow
                };

                _context.LessonProgresses.Add(progress);
            }

            progress.WatchPercentage = request.WatchPercentage;
            progress.LastPosition = request.LastPosition;
            progress.TimeSpentMinutes += request.TimeSpentMinutes;

            if (progress.StartedAt == null)
                progress.StartedAt = DateTime.UtcNow;

            if (request.WatchPercentage >= 100)
            {
                progress.Status = "Completed";
                progress.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                progress.Status = "InProgress";
            }

            // update enrollment
            enrollment.LastAccessedAt = DateTime.UtcNow;

            if (enrollment.StartedAt == null)
                enrollment.StartedAt = DateTime.UtcNow;

            // calculate course progress
            var totalLessons = await _context.Lessons
                .CountAsync(l => l.CourseId == enrollment.CourseId && !l.IsDeleted, cancellationToken);

            var completedLessons = await _context.LessonProgresses
                .CountAsync(p =>
                    p.EnrollmentId == enrollment.Id &&
                    p.Status == "Completed",
                    cancellationToken);

            enrollment.Progress = totalLessons == 0
                ? 0
                : (completedLessons * 100) / totalLessons;

            if (enrollment.Progress == 100)
            {
                enrollment.Status = "Completed";
                enrollment.CompletedAt = DateTime.UtcNow;
            }
            else if (enrollment.Progress > 0)
            {
                enrollment.Status = "InProgress";
            }

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}