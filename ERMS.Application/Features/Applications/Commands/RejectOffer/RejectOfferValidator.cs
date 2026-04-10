using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.RejectOffer;

public sealed class RejectOfferValidator : AbstractValidator<RejectOfferCommand>
{
    public RejectOfferValidator()
    {
        RuleFor(x => x.OfferId)
            .NotEmpty()
            .WithMessage("OfferId là bắt buộc.");

        RuleFor(x => x.CandidateNote)
            .NotEmpty()
            .WithMessage("Lý do từ chối là bắt buộc.")
            .MaximumLength(1000)
            .WithMessage("Ghi chú không được vượt quá 1000 ký tự.");
    }
}
