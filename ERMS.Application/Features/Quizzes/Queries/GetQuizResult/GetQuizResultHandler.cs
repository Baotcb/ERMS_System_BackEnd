using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Quizzes.Queries.GetQuizResult;

public sealed class GetQuizResultHandler
    : IRequestHandler<GetQuizResultQuery, GetQuizResultResponse?>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetQuizResultHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetQuizResultResponse?> Handle(
        GetQuizResultQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return null;

        var employee = await _context.Employees
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (employee == null) return null;

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(x =>
                x.EmployeeId == employee.Id &&
                x.CourseId == request.CourseId &&
                !x.IsDeleted,
                cancellationToken);
        if (enrollment == null) return null;

        // Get the quiz for this course
        var quiz = await _context.Quizzes
            .FirstOrDefaultAsync(x =>
                x.CourseId == request.CourseId &&
                x.IsActive &&
                !x.IsDeleted,
                cancellationToken);
        if (quiz == null) return null;

        var attemptCount = await _context.QuizAttempts
             .CountAsync(x => x.QuizId == quiz.Id && x.EnrollmentId == enrollment.Id, cancellationToken);

        var latestAttempt = await _context.QuizAttempts
    .Where(x => x.QuizId == quiz.Id && x.EnrollmentId == enrollment.Id)
    .OrderByDescending(x => x.StartedAt)
    .FirstOrDefaultAsync(cancellationToken);

        var NextAvailableTime = latestAttempt != null && quiz.TimeLimitMinutes.HasValue
            ? latestAttempt.StartedAt.AddMinutes(quiz.TimeLimitMinutes.Value)
            : (DateTime?)null;

        return new GetQuizResultResponse
        {
            AttemptId = latestAttempt.Id,
            Score = latestAttempt.Score ?? 0,
            IsPassed = latestAttempt.IsPassed ?? false,
            CorrectAnswers = latestAttempt.CorrectAnswers ?? 0,
            TotalQuestions = latestAttempt.TotalQuestions,
            AttemptCount = attemptCount,
            MaxAttempts = quiz.MaxAttempts,
            NextAvailableTime = NextAvailableTime,
            CompletedAt = latestAttempt.CompletedAt
        };
    }
}
