using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Quizzes.Commands.SubmitQuiz;

public sealed class SubmitQuizCommandHandler
    : IRequestHandler<SubmitQuizCommand, QuizResultDto>
{
    private readonly IERMSDbContext _context;

    public SubmitQuizCommandHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<QuizResultDto> Handle(
        SubmitQuizCommand request,
        CancellationToken cancellationToken)
    {
        var attempt = await _context.QuizAttempts
            .Include(x => x.Quiz)
            .ThenInclude(x => x.Questions)
            .Include(x => x.QuizAnswers)
            .FirstOrDefaultAsync(x => x.Id == request.AttemptId, cancellationToken);

        if (attempt == null)
            throw new Exception("Không tìm thấy lượt làm bài");

        int correct = 0;

        foreach (var answer in attempt.QuizAnswers)
        {
            var question = attempt.Quiz.Questions
                .First(x => x.Id == answer.QuizQuestionId);

            if (answer.SelectedAnswer == question.CorrectAnswer)
            {
                answer.IsCorrect = true;
                answer.PointsEarned = question.Points;
                correct++;
            }
        }

        var score = (decimal)correct * 100 / attempt.TotalQuestions;

        attempt.Score = score;
        attempt.CorrectAnswers = correct;
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.IsPassed = score >= attempt.Quiz.PassingScore;
        attempt.Status = "Completed";

        await _context.SaveChangesAsync(cancellationToken);

        return new QuizResultDto
        {
            Score = score,
            CorrectAnswers = correct,
            TotalQuestions = attempt.TotalQuestions,
            IsPassed = attempt.IsPassed ?? false
        };
    }
}