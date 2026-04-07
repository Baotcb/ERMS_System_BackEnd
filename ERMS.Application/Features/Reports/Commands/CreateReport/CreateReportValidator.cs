using ERMS.Domain.Constants.System;
using FluentValidation;

namespace ERMS.Application.Features.Reports.Commands.CreateReport;

public sealed class CreateReportValidator : AbstractValidator<CreateReportCommand>
{
    private static readonly HashSet<string> AllowedEntityTypes =
    [
        ReportConstants.EntityType.JobPosting,
        ReportConstants.EntityType.Enterprise
    ];

    private static readonly HashSet<string> AllowedReasons =
    [
        ReportConstants.Reason.FraudulentInfo,
        ReportConstants.Reason.InappropriateContent,
        ReportConstants.Reason.LaborLawViolation,
        ReportConstants.Reason.IllegalFeeCollection,
        ReportConstants.Reason.FakeContactInfo,
        ReportConstants.Reason.Other
    ];

    public CreateReportValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty()
            .Must(AllowedEntityTypes.Contains)
            .WithMessage("EntityType must be 'JobPosting' or 'Enterprise'.");

        RuleFor(x => x.EntityId)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(100)
            .Must(AllowedReasons.Contains)
            .WithMessage("Reason is not valid.");

        RuleFor(x => x.Description)
            .MaximumLength(2000);
    }
}

