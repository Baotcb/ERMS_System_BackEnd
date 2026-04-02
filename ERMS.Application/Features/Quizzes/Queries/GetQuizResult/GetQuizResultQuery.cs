using MediatR;

namespace ERMS.Application.Features.Quizzes.Queries.GetQuizResult;

public sealed class GetQuizResultQuery : IRequest<GetQuizResultResponse?>
{
    public Guid CourseId { get; set; }
}

public sealed class GetQuizResultResponse
{
    public Guid AttemptId { get; set; }
    public decimal Score { get; set; }
    public bool IsPassed { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public int AttemptCount { get; set; }
    public int? MaxAttempts { get; set; }
    public DateTime? CompletedAt { get; set; }
}
