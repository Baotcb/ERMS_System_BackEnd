using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Courses.Queries.GetCourseProgress;

public class GetCourseProgressHandler
    : IRequestHandler<GetCourseProgressQuery, CourseProgressDto>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCourseProgressHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CourseProgressDto> Handle(
        GetCourseProgressQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId == null)
            throw new UnauthorizedAccessException();

        var employee = await _context.Employees
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (employee == null)
            throw new Exception("Employee not found");

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(x =>
                x.EmployeeId == employee.Id &&
                x.CourseId == request.CourseId &&
                !x.IsDeleted,
                cancellationToken);

        if (enrollment == null)
            throw new Exception("User is not enrolled in this course");

        var totalLessons = await _context.Lessons
            .CountAsync(x =>
                x.CourseId == request.CourseId &&
                !x.IsDeleted,
                cancellationToken);

        var completedLessons = await _context.LessonProgresses
            .Where(x =>
                x.EnrollmentId == enrollment.Id &&
                x.Status == "Completed")
            .Join(
                _context.Lessons,
                p => p.LessonId,
                l => l.Id,
                (p, l) => l
            )
            .CountAsync(x =>
                x.CourseId == request.CourseId &&
                !x.IsDeleted,
                cancellationToken);

        var progress = totalLessons == 0
            ? 0
            : (completedLessons * 100) / totalLessons;

        // For offline courses: quiz unlocked only when HR confirms workshop
        // For online courses: quiz unlocked when all lessons completed
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

        bool quizUnlocked;
        if (course != null && !course.IsOnline)
        {
            // Offline course: check workshop confirmation
            quizUnlocked = await _context.WorkshopConfirmations
                .AnyAsync(w => w.CourseId == request.CourseId, cancellationToken);
        }
        else
        {
            // Online course: all lessons must be completed
            quizUnlocked = completedLessons == totalLessons;
        }

        return new CourseProgressDto
        {
            TotalLessons = totalLessons,
            CompletedLessons = completedLessons,
            ProgressPercentage = progress,
            QuizUnlocked = quizUnlocked
        };
    }
}