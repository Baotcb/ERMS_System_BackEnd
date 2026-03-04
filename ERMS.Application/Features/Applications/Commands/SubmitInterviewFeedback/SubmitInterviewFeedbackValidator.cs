using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.SubmitInterviewFeedback;

/// <summary>
/// Validator for SubmitInterviewFeedbackCommand
/// </summary>
public sealed class SubmitInterviewFeedbackValidator : AbstractValidator<SubmitInterviewFeedbackCommand>
{
    private const int MaxFeedbackLength = 2000;
    private const int MaxRecommendationLength = 1000;

    public SubmitInterviewFeedbackValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId is required.");

        RuleFor(x => x.InterviewId)
            .NotEmpty()
            .WithMessage("InterviewId is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Feedback)
            .NotEmpty()
            .WithMessage("Feedback is required.")
            .MaximumLength(MaxFeedbackLength)
            .WithMessage($"Feedback must not exceed {MaxFeedbackLength} characters.");

        RuleFor(x => x.Recommendation)
            .MaximumLength(MaxRecommendationLength)
            .When(x => !string.IsNullOrEmpty(x.Recommendation))
            .WithMessage($"Recommendation must not exceed {MaxRecommendationLength} characters.");
    }
}
