using FluentValidation;

namespace ERMS.Application.Features.Reports.Commands.ProcessReport;

public sealed class ProcessReportValidator : AbstractValidator<ProcessReportCommand>
{
    private static readonly HashSet<string> AllowedActions =
    [
        "Dismiss",
        "Resolve",
        "HideJobPosting",
        "SuspendEnterprise"
    ];

    public ProcessReportValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty();

        RuleFor(x => x.Action)
            .NotEmpty()
            .Must(AllowedActions.Contains)
            .WithMessage("Action is not valid.");

        RuleFor(x => x.AdminNote)
            .MaximumLength(2000);
    }
}

