using FluentValidation;
using ERMS.Domain.Constants.Application;

namespace ERMS.Application.Features.Applications.Queries.GetApplicationsByJob;

/// <summary>
/// Validator for GetApplicationsByJobQuery
/// </summary>
public sealed class GetApplicationsByJobValidator : AbstractValidator<GetApplicationsByJobQuery>
{
    public GetApplicationsByJobValidator()
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

        RuleFor(x => x.StageFilter)
            .Must(stage => string.IsNullOrEmpty(stage) || ApplicationStage.IsValid(stage))
            .WithMessage("StageFilter must be a valid application stage.");
    }
}
