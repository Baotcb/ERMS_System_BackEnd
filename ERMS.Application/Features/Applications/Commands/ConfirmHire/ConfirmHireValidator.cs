using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.ConfirmHire;

public sealed class ConfirmHireValidator : AbstractValidator<ConfirmHireCommand>
{
    public ConfirmHireValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId là bắt buộc.");

        RuleFor(x => x.EmployeeEmail)
            .NotEmpty()
            .WithMessage("Email nhân viên là bắt buộc.")
            .EmailAddress()
            .WithMessage("Email nhân viên phải là địa chỉ email hợp lệ.");
    }
}
