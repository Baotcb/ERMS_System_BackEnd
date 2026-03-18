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
            .WithMessage("ApplicationId là bắt buộc.");

        RuleFor(x => x.InterviewId)
            .NotEmpty()
            .WithMessage("InterviewId là bắt buộc.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Điểm đánh giá phải từ 1 đến 5.");

        RuleFor(x => x.Feedback)
            .NotEmpty()
            .WithMessage("Nhận xét là bắt buộc.")
            .MaximumLength(MaxFeedbackLength)
            .WithMessage($"Nhận xét không được vượt quá {MaxFeedbackLength} ký tự.");

        RuleFor(x => x.Recommendation)
            .MaximumLength(MaxRecommendationLength)
            .When(x => !string.IsNullOrEmpty(x.Recommendation))
            .WithMessage($"Đề xuất không được vượt quá {MaxRecommendationLength} ký tự.");
    }
}
