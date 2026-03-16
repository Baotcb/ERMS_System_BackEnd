using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Quizzes.Commands.StartQuiz;

public sealed class StartQuizCommandHandler
    : IRequestHandler<StartQuizCommand, Guid>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public StartQuizCommandHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(StartQuizCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực");
        var employee = await _context.Employees
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var enrollment = await _context.Enrollments
    .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id && !x.IsDeleted, cancellationToken);

        if (enrollment == null)
            throw new Exception("Người dùng chưa đăng ký khóa học");

        // CHECK COURSE COMPLETION
        if (enrollment.Status != "Completed")
        {
            throw new Exception("Bạn phải hoàn thành khóa học trước khi làm bài kiểm tra.");
        }

        var quiz = await _context.Quizzes
    .Include(x => x.Questions)
    .FirstOrDefaultAsync(x =>
        x.Id == request.QuizId &&
        x.IsActive &&
        !x.IsDeleted,
        cancellationToken);

        if (quiz == null)
            throw new Exception("Không tìm thấy bài kiểm tra");

        // CHECK LESSON COMPLETION

        var totalLessons = await _context.Lessons
            .CountAsync(x =>
                x.CourseId == quiz.CourseId &&
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
                x.CourseId == quiz.CourseId &&
                !x.IsDeleted,
                cancellationToken);

        if (completedLessons < totalLessons)
        {
            throw new Exception(
                $"You must complete all lessons before starting the quiz ({completedLessons}/{totalLessons})");
        }

        var attemptCount = await _context.QuizAttempts
            .CountAsync(x => x.QuizId == quiz.Id && x.EnrollmentId == enrollment.Id, cancellationToken);

        if (quiz.MaxAttempts.HasValue && attemptCount >= quiz.MaxAttempts)
            throw new Exception("Đã đạt tối đa số lần làm bài");

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            EnrollmentId = enrollment.Id,
            AttemptNumber = attemptCount + 1,
            TotalQuestions = quiz.Questions.Count,
            StartedAt = DateTime.UtcNow,
            Status = "InProgress"
        };

        _context.QuizAttempts.Add(attempt);

        await _context.SaveChangesAsync(cancellationToken);

        return attempt.Id;
    }
}