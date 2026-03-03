using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.ConfirmHire;

public sealed class ConfirmHireValidator : AbstractValidator<ConfirmHireCommand>
{
    public ConfirmHireValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId is required.");

        RuleFor(x => x.EmployeeEmail)
            .NotEmpty()
            .WithMessage("EmployeeEmail is required.")
            .EmailAddress()
            .WithMessage("EmployeeEmail must be a valid email address.");
    }
}
