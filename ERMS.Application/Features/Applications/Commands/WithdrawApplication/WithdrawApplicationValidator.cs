using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.WithdrawApplication;

/// <summary>
/// Validator for WithdrawApplicationCommand
/// </summary>
public sealed class WithdrawApplicationValidator : AbstractValidator<WithdrawApplicationCommand>
{
    private const int MaxReasonLength = 2000;

    public WithdrawApplicationValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId là bắt buộc.");

        RuleFor(x => x.Reason)
            .MaximumLength(MaxReasonLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Reason))
            .WithMessage($"Lý do rút hồ sơ không được vượt quá {MaxReasonLength} ký tự.");
    }
}
