using MediatR;

namespace ERMS.Application.Features.Quizzes.Commands.StartQuiz;

public sealed class StartQuizCommand : IRequest<Guid>
{
    public Guid QuizId { get; set; }
}