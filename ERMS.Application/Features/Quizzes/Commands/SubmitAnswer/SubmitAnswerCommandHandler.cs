using ERMS.Application.Features.Quizzes.Commands.SubmitAnswer;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;

public sealed class SubmitAnswerCommandHandler
    : IRequestHandler<SubmitAnswerCommand>
{
    private readonly IERMSDbContext _context;

    public SubmitAnswerCommandHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        SubmitAnswerCommand request,
        CancellationToken cancellationToken)
    {
        var answer = new QuizAnswer
        {
            Id = Guid.NewGuid(),
            QuizAttemptId = request.AttemptId,
            QuizQuestionId = request.QuestionId,
            SelectedAnswer = request.SelectedAnswer,
            AnsweredAt = DateTime.UtcNow
        };

        _context.QuizAnswers.Add(answer);

        await _context.SaveChangesAsync(cancellationToken);
    }
}