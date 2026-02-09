using FluentValidation;

namespace ERMS.Application.Features.Applications.Queries.GetShortlistedApplications;

/// <summary>
/// Validator for GetShortlistedApplicationsQuery
/// </summary>
public sealed class GetShortlistedApplicationsValidator : AbstractValidator<GetShortlistedApplicationsQuery>
{
    public GetShortlistedApplicationsValidator()
    {
        RuleFor(x => x.JobPostingId)
            .NotEmpty()
            .WithMessage("JobPostingId is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}
