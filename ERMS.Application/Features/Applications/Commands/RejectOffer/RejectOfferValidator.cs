using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.RejectOffer;

public sealed class RejectOfferValidator : AbstractValidator<RejectOfferCommand>
{
    public RejectOfferValidator()
    {
        RuleFor(x => x.OfferId)
            .NotEmpty()
            .WithMessage("OfferId is required.");

        RuleFor(x => x.CandidateNote)
            .MaximumLength(1000)
            .WithMessage("CandidateNote must not exceed 1000 characters.");
    }
}
