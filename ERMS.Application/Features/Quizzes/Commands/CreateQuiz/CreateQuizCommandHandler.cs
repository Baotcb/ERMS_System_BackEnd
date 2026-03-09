using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Quizzes.Commands.CreateQuiz;

public sealed class CreateQuizCommandHandler
    : IRequestHandler<CreateQuizCommand, Guid>
{
    private readonly IERMSDbContext _context;

    public CreateQuizCommandHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(
        CreateQuizCommand request,
        CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .FirstOrDefaultAsync(x => x.Id == request.CourseId && !x.IsDeleted, cancellationToken);

        if (course == null)
            throw new Exception("Course not found");

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            CourseId = request.CourseId,
            QuizTitle = request.QuizTitle,
            Description = request.Description,
            TimeLimitMinutes = request.TimeLimitMinutes,
            PassingScore = request.PassingScore,
            MaxAttempts = request.MaxAttempts,
            ShuffleQuestions = request.ShuffleQuestions,
            ShuffleAnswers = request.ShuffleAnswers,
            ShowCorrectAnswers = request.ShowCorrectAnswers,
            IsActive = true
        };

        _context.Quizzes.Add(quiz);

        await _context.SaveChangesAsync(cancellationToken);

        return quiz.Id;
    }
}