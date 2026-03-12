using MediatR;

namespace ERMS.Application.Features.Quizzes.Commands.CreateQuizQuestion;

public sealed class CreateQuizQuestionCommand : IRequest<Guid>
{
    public Guid QuizId { get; set; }

    public string QuestionText { get; set; } = null!;

    public string Options { get; set; } = null!;

    public string CorrectAnswer { get; set; } = null!;

    public string? Explanation { get; set; }

    public int Points { get; set; } = 1;

    public int OrderIndex { get; set; }
}