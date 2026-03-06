using FluentValidation;

namespace ERMS.Application.Features.Applications.Queries.GetMyApplications;

public sealed class GetMyApplicationsValidator : AbstractValidator<GetMyApplicationsQuery>
{
    public GetMyApplicationsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("PageSize must be between 1 and 50.");
    }
}
