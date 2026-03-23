using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.RejectApplication;

/// <summary>
/// Validator for RejectApplicationCommand.
/// </summary>
public sealed class RejectApplicationValidator : AbstractValidator<RejectApplicationCommand>
{
    private const int MaxReasonLength = 2000;

    public RejectApplicationValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId là bắt buộc.");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("Lý do từ chối là bắt buộc.")
            .MaximumLength(MaxReasonLength)
            .WithMessage($"Lý do từ chối không được vượt quá {MaxReasonLength} ký tự.");
    }
}
