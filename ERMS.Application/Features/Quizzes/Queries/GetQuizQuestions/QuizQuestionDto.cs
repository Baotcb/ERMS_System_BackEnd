namespace ERMS.Application.Features.Quizzes.Queries.GetQuizQuestions;

public class QuizQuestionDto
{
    public Guid Id { get; set; }

    public string QuestionText { get; set; } = null!;

    public string Options { get; set; } = null!;

    public int OrderIndex { get; set; }
}