using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Quizzes.Commands.CreateQuizQuestion;

public sealed class CreateQuizQuestionHandler
    : IRequestHandler<CreateQuizQuestionCommand, Guid>
{
    private readonly IERMSDbContext _context;

    public CreateQuizQuestionHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(
        CreateQuizQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var quiz = await _context.Quizzes
            .FirstOrDefaultAsync(x => x.Id == request.QuizId && !x.IsDeleted, cancellationToken);

        if (quiz == null)
            throw new Exception("Không tìm thấy bài kiểm tra");

        var question = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            QuizId = request.QuizId,
            QuestionText = request.QuestionText,
            Options = request.Options,
            CorrectAnswer = request.CorrectAnswer,
            Explanation = request.Explanation,
            Points = request.Points,
            OrderIndex = request.OrderIndex,
            IsActive = true
        };

        _context.QuizQuestions.Add(question);

        await _context.SaveChangesAsync(cancellationToken);

        return question.Id;
    }
}