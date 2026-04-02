using MediatR;

namespace ERMS.Application.Features.Quizzes.Queries.GetQuizReview;

public sealed class GetQuizReviewQuery : IRequest<QuizReviewDto>
{
    public Guid AttemptId { get; set; }
}

public sealed class QuizReviewDto
{
    public decimal Score { get; set; }
    public bool IsPassed { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public List<QuizReviewItemDto> Items { get; set; } = new();
}

public sealed class QuizReviewItemDto
{
    public int OrderIndex { get; set; }
    public string QuestionText { get; set; } = null!;
    public string Options { get; set; } = null!;
    public string? SelectedAnswer { get; set; }
    public string CorrectAnswer { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
}
