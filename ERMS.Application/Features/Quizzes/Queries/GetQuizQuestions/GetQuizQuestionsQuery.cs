using MediatR;

namespace ERMS.Application.Features.Quizzes.Queries.GetQuizQuestions;

public sealed class GetQuizQuestionsQuery : IRequest<List<QuizQuestionDto>>
{
    public Guid AttemptId { get; set; }
}