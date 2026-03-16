using MediatR;

namespace ERMS.Application.Features.Quizzes.Commands.SubmitQuiz;

public sealed class SubmitQuizCommand : IRequest<QuizResultDto>
{
    public Guid AttemptId { get; set; }
}