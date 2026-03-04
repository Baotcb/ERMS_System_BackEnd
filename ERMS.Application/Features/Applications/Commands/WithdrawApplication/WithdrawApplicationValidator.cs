using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.WithdrawApplication;

/// <summary>
/// Validator for WithdrawApplicationCommand
/// </summary>
public sealed class WithdrawApplicationValidator : AbstractValidator<WithdrawApplicationCommand>
{
    public WithdrawApplicationValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId is required.");
    }
}
