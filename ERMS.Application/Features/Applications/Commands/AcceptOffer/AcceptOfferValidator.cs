using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.AcceptOffer;

public sealed class AcceptOfferValidator : AbstractValidator<AcceptOfferCommand>
{
    public AcceptOfferValidator()
    {
        RuleFor(x => x.OfferId)
            .NotEmpty()
            .WithMessage("OfferId is required.");
    }
}
