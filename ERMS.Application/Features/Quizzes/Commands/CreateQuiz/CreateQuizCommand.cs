using MediatR;

namespace ERMS.Application.Features.Quizzes.Commands.CreateQuiz;

public sealed class CreateQuizCommand : IRequest<Guid>
{
    public Guid CourseId { get; set; }

    public string QuizTitle { get; set; } = null!;

    public string? Description { get; set; }

    public int? TimeLimitMinutes { get; set; }

    public int PassingScore { get; set; } = 80;

    public int? MaxAttempts { get; set; }

    public bool ShuffleQuestions { get; set; } = true;

    public bool ShuffleAnswers { get; set; } = true;

    public bool ShowCorrectAnswers { get; set; }
}