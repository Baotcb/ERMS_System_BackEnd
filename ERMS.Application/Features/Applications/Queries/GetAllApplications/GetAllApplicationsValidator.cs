using FluentValidation;
using ERMS.Domain.Constants.Application;

namespace ERMS.Application.Features.Applications.Queries.GetAllApplications;

/// <summary>
/// Validator for GetAllApplicationsQuery
/// </summary>
public sealed class GetAllApplicationsValidator : AbstractValidator<GetAllApplicationsQuery>
{
    public GetAllApplicationsValidator()
    {
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
