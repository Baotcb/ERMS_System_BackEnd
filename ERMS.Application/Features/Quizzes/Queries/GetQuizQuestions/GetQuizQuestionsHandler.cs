using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Quizzes.Queries.GetQuizQuestions;

public sealed class GetQuizQuestionsHandler
    : IRequestHandler<GetQuizQuestionsQuery, List<QuizQuestionDto>>
{
    private readonly IERMSDbContext _context;

    public GetQuizQuestionsHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<List<QuizQuestionDto>> Handle(
        GetQuizQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var attempt = await _context.QuizAttempts
            .Include(x => x.Quiz)
            .ThenInclude(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == request.AttemptId, cancellationToken);

        if (attempt == null)
            throw new Exception("Không tìm thấy lượt làm bài");

        return attempt.Quiz.Questions
            .Where(x => x.IsActive)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new QuizQuestionDto
            {
                Id = x.Id,
                QuestionText = x.QuestionText,
                Options = x.Options,
                OrderIndex = x.OrderIndex
            })
            .ToList();
    }
}