namespace ERMS.Application.Features.Quizzes.Commands.SubmitQuiz;

public class QuizResultDto
{
    public decimal Score { get; set; }

    public bool IsPassed { get; set; }

    public int CorrectAnswers { get; set; }

    public int TotalQuestions { get; set; }
}