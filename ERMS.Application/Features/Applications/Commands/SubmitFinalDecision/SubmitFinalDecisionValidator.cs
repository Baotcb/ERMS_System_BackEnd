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
            .WithMessage("ApplicationId is required.");

        RuleFor(x => x.InterviewId)
            .NotEmpty()
            .WithMessage("InterviewId is required.");

        RuleFor(x => x.Decision)
            .NotEmpty()
            .WithMessage("Decision is required.")
            .Must(d => InterviewDecision.IsValid(d))
            .WithMessage($"Decision must be one of: {string.Join(", ", InterviewDecision.ValidDecisions)}.");

        RuleFor(x => x.OverallRating)
            .InclusiveBetween(1, 5)
            .When(x => x.OverallRating.HasValue)
            .WithMessage("OverallRating must be between 1 and 5.");

        RuleFor(x => x.OverallFeedback)
            .MaximumLength(MaxFeedbackLength)
            .When(x => !string.IsNullOrEmpty(x.OverallFeedback))
            .WithMessage($"OverallFeedback must not exceed {MaxFeedbackLength} characters.");

        RuleFor(x => x.Note)
            .MaximumLength(MaxNoteLength)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage($"Note must not exceed {MaxNoteLength} characters.");

        RuleFor(x => x.NextRoundInterviewerIds)
            .Must(ids => ids == null || ids.All(id => id != Guid.Empty))
            .WithMessage("NextRoundInterviewerIds cannot contain empty GUIDs.");
    }
}
