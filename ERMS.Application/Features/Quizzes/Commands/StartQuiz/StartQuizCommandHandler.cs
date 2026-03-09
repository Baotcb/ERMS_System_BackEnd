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
            throw new UnauthorizedAccessException("User not authenticated");
        var employee = await _context.Employees
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id && !x.IsDeleted, cancellationToken);

        if (enrollment == null)
            throw new Exception("User is not enrolled");

        var quiz = await _context.Quizzes
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == request.QuizId && x.IsActive && !x.IsDeleted, cancellationToken);

        if (quiz == null)
            throw new Exception("Quiz not found");

        var attemptCount = await _context.QuizAttempts
            .CountAsync(x => x.QuizId == quiz.Id && x.EnrollmentId == enrollment.Id, cancellationToken);

        if (quiz.MaxAttempts.HasValue && attemptCount >= quiz.MaxAttempts)
            throw new Exception("Max attempts reached");

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