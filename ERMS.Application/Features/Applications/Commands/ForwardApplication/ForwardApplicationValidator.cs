using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.ForwardApplication;

/// <summary>
/// Validator for ForwardApplicationCommand
/// </summary>
public sealed class ForwardApplicationValidator : AbstractValidator<ForwardApplicationCommand>
{
    private const int MaxHRNoteLength = 2000;

    public ForwardApplicationValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId is required.");

        RuleFor(x => x.HRNote)
            .MaximumLength(MaxHRNoteLength)
            .When(x => !string.IsNullOrEmpty(x.HRNote))
            .WithMessage($"HR Note must not exceed {MaxHRNoteLength} characters.");
    }
}
