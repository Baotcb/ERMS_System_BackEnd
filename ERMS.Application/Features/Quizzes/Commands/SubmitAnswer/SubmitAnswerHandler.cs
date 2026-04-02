using ERMS.Application.Features.Quizzes.Commands.SubmitAnswer;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class SubmitAnswerHandler
    : IRequestHandler<SubmitAnswerCommand>
{
    private readonly IERMSDbContext _context;

    public SubmitAnswerHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        SubmitAnswerCommand request,
        CancellationToken cancellationToken)
    {
        // Upsert: nếu đã có answer cho câu hỏi này trong lượt làm này → cập nhật
        var existing = await _context.QuizAnswers
            .FirstOrDefaultAsync(a =>
                a.QuizAttemptId == request.AttemptId &&
                a.QuizQuestionId == request.QuestionId,
                cancellationToken);

        if (existing != null)
        {
            existing.SelectedAnswer = request.SelectedAnswer;
            existing.AnsweredAt = DateTime.UtcNow;
            existing.IsCorrect = null;     // reset — sẽ tính lại khi submit
            existing.PointsEarned = 0;     // reset
        }
        else
        {
            _context.QuizAnswers.Add(new QuizAnswer
            {
                Id = Guid.NewGuid(),
                QuizAttemptId = request.AttemptId,
                QuizQuestionId = request.QuestionId,
                SelectedAnswer = request.SelectedAnswer,
                AnsweredAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}