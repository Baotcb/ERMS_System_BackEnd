using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Quizzes.Commands.SubmitQuiz;

public sealed class SubmitQuizHandler
    : IRequestHandler<SubmitQuizCommand, QuizResultDto>
{
    private readonly IERMSDbContext _context;

    public SubmitQuizHandler(IERMSDbContext context)
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

        // Deduplicate: chỉ lấy câu trả lời mới nhất cho mỗi câu hỏi (phòng trường hợp data cũ bị trùng)
        var uniqueAnswers = attempt.QuizAnswers
            .GroupBy(a => a.QuizQuestionId)
            .Select(g => g.OrderByDescending(a => a.AnsweredAt).First())
            .ToList();

        foreach (var answer in uniqueAnswers)
        {
            var question = attempt.Quiz.Questions
                .FirstOrDefault(x => x.Id == answer.QuizQuestionId);

            if (question == null)
                continue; // Question deleted after quiz started — skip safely

            if (answer.SelectedAnswer == question.CorrectAnswer)
            {
                answer.IsCorrect = true;
                answer.PointsEarned = question.Points;
                correct++;
            }
            else
            {
                answer.IsCorrect = false;
                answer.PointsEarned = 0;
            }
        }

        var totalQ = attempt.TotalQuestions > 0 ? attempt.TotalQuestions : uniqueAnswers.Count;
        var score = totalQ > 0 ? (decimal)correct * 100 / totalQ : 0;

        attempt.Score = score;
        attempt.CorrectAnswers = correct;
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.IsPassed = score >= attempt.Quiz.PassingScore;
        attempt.Status = "Completed";

        if (attempt.IsPassed == true)
        {
            var enrollment = await _context.Enrollments
                .Include(x => x.Course)
                .FirstOrDefaultAsync(x => x.Id == attempt.EnrollmentId, cancellationToken);

            if (enrollment != null && enrollment.Status != "Completed")
            {
                enrollment.Status = "Completed";
                enrollment.CompletedAt = DateTime.UtcNow;
            }

            //  Lấy danh sách skill từ course
            var courseSkills = await _context.CourseSkills
                .Include(x => x.Skill)
                .Where(x => x.CourseId == enrollment.CourseId)
                .ToListAsync(cancellationToken);

            //  Lấy employee
            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == enrollment.EmployeeId, cancellationToken);

            if (employee != null && courseSkills.Any())
            {
                var newSkills = courseSkills
                    .Select(x => x.Skill.SkillName) // giả sử Skill có Name
                    .ToList();

                //  nếu chưa có thì thêm (tránh trùng)
                var currentSkills = string.IsNullOrEmpty(employee.SkillDescription)
                    ? new List<string>()
                    : employee.SkillDescription.Split(',').Select(s => s.Trim()).ToList();

                foreach (var skill in newSkills)
                {
                    if (!currentSkills.Contains(skill))
                    {
                        currentSkills.Add(skill);
                    }
                }

                employee.SkillDescription = string.Join(", ", currentSkills);
            }
        }
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