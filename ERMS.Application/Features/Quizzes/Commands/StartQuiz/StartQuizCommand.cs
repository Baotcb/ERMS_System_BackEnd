using MediatR;

namespace ERMS.Application.Features.Quizzes.Commands.StartQuiz;

public sealed class StartQuizResult
{
    public Guid AttemptId { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public int? MaxAttempts { get; set; }
    public int PassingScore { get; set; }
    public int TotalQuestions { get; set; }
}

public sealed class StartQuizCommand : IRequest<StartQuizResult>
{
    public Guid? QuizId { get; set; }

    public Guid? CourseId { get; set; }
}