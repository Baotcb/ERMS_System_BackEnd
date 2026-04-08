using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.CancelOffer;

public sealed class CancelOfferValidator : AbstractValidator<CancelOfferCommand>
{
    public CancelOfferValidator()
    {
        RuleFor(x => x.OfferId)
            .NotEmpty()
            .WithMessage("OfferId là bắt buộc.");

        RuleFor(x => x.CancellationReason)
            .NotEmpty()
            .WithMessage("Lý do hủy offer là bắt buộc.")
            .MaximumLength(1000)
            .WithMessage("Lý do hủy không được vượt quá 1000 ký tự.");
    }
}
