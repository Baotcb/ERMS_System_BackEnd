using FluentValidation;

namespace ERMS.Application.Features.Applications.Queries.GetMyOffers;

public sealed class GetMyOffersValidator : AbstractValidator<GetMyOffersQuery>
{
    public GetMyOffersValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("PageSize must be between 1 and 50.");
    }
}
