using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.RespondOfferByToken;

public sealed class RespondOfferByTokenValidator : AbstractValidator<RespondOfferByTokenCommand>
{
    public RespondOfferByTokenValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token là bắt buộc.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action là bắt buộc.")
            .Must(a => a.Equals("accept", StringComparison.OrdinalIgnoreCase) || a.Equals("reject", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Action phải là 'accept' hoặc 'reject'.");
    }
}
