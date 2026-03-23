using ERMS.Domain.Constants.Application;
using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.SubmitFinalDecision;

/// <summary>
/// Validator for SubmitFinalDecisionCommand
/// </summary>
public sealed class SubmitFinalDecisionValidator : AbstractValidator<SubmitFinalDecisionCommand>
{
    private const int MaxFeedbackLength = 2000;
    private const int MaxNoteLength = 2000;

    public SubmitFinalDecisionValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId là bắt buộc.");

        RuleFor(x => x.InterviewId)
            .NotEmpty()
            .WithMessage("InterviewId là bắt buộc.");

        RuleFor(x => x.Decision)
            .NotEmpty()
            .WithMessage("Quyết định là bắt buộc.")
            .Must(InterviewDecision.IsValid)
            .WithMessage($"Quyết định phải là một trong: {string.Join(", ", InterviewDecision.ValidDecisions)}.");

        RuleFor(x => x.OverallRating)
            .InclusiveBetween(1, 5)
            .When(x => x.OverallRating.HasValue)
            .WithMessage("Điểm đánh giá tổng thể phải từ 1 đến 5.");

        RuleFor(x => x.OverallFeedback)
            .MaximumLength(MaxFeedbackLength)
            .When(x => !string.IsNullOrEmpty(x.OverallFeedback))
            .WithMessage($"Đánh giá tổng thể không được vượt quá {MaxFeedbackLength} ký tự.");

        RuleFor(x => x.OverallFeedback)
            .NotEmpty()
            .When(x => string.Equals(x.Decision, InterviewDecision.Fail, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Phải nhập đánh giá tổng quan khi từ chối ứng viên.");

        RuleFor(x => x.Note)
            .MaximumLength(MaxNoteLength)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage($"Ghi chú không được vượt quá {MaxNoteLength} ký tự.");

        RuleFor(x => x.NextRoundInterviewerIds)
            .Must(ids => ids == null || ids.All(id => id != Guid.Empty))
            .WithMessage("NextRoundInterviewerIds không được chứa GUID rỗng.");
    }
}
