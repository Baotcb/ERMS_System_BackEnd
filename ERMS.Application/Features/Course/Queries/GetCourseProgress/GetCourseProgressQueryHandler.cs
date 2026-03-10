using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Courses.Queries.GetCourseProgress;

public class GetCourseProgressQueryHandler
    : IRequestHandler<GetCourseProgressQuery, CourseProgressDto>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCourseProgressQueryHandler(
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

        return new CourseProgressDto
        {
            TotalLessons = totalLessons,
            CompletedLessons = completedLessons,
            ProgressPercentage = progress,
            QuizUnlocked = completedLessons == totalLessons
        };
    }
}