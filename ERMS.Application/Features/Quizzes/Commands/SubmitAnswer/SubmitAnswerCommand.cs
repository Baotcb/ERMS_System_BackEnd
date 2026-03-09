using MediatR;

namespace ERMS.Application.Features.Quizzes.Commands.SubmitAnswer;

public sealed class SubmitAnswerCommand : IRequest
{
    public Guid AttemptId { get; set; }

    public Guid QuestionId { get; set; }

    public string SelectedAnswer { get; set; } = null!;
}