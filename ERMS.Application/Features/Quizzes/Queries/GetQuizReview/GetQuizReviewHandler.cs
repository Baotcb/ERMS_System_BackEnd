using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Quizzes.Queries.GetQuizReview;

public sealed class GetQuizReviewHandler
    : IRequestHandler<GetQuizReviewQuery, QuizReviewDto>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetQuizReviewHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<QuizReviewDto> Handle(
        GetQuizReviewQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");

        var attempt = await _context.QuizAttempts
            .Include(a => a.Enrollment)
                .ThenInclude(e => e.Employee)
            .Include(a => a.QuizAnswers)
                .ThenInclude(a => a.QuizQuestion)
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy lượt làm bài.");

        if (attempt.Enrollment.Employee.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền xem bài làm này.");

        if (attempt.Status != "Completed")
            throw new InvalidOperationException("Bài làm chưa hoàn thành, không thể xem review.");

        var items = attempt.QuizAnswers
            .Where(a => a.QuizQuestion != null)
            .OrderBy(a => a.QuizQuestion.OrderIndex)
            .Select(a => new QuizReviewItemDto
            {
                OrderIndex = a.QuizQuestion.OrderIndex,
                QuestionText = a.QuizQuestion.QuestionText,
                Options = a.QuizQuestion.Options,
                SelectedAnswer = a.SelectedAnswer,
                CorrectAnswer = a.QuizQuestion.CorrectAnswer,
                IsCorrect = a.IsCorrect ?? false,
                Explanation = a.QuizQuestion.Explanation,
            })
            .ToList();

        return new QuizReviewDto
        {
            Score = attempt.Score ?? 0,
            IsPassed = attempt.IsPassed ?? false,
            CorrectAnswers = attempt.CorrectAnswers ?? 0,
            TotalQuestions = attempt.TotalQuestions,
            Items = items,
        };
    }
}

