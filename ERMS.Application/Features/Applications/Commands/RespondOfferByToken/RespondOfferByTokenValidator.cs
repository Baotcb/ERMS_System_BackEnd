using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.RespondOfferByToken;

public sealed class RespondOfferByTokenValidator : AbstractValidator<RespondOfferByTokenCommand>
{
    private static readonly string[] ValidActions = ["accept", "reject"];

    public RespondOfferByTokenValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token không hợp lệ.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Hành động không được để trống.")
            .Must(a => ValidActions.Contains(a.ToLower()))
            .WithMessage("Hành động phải là 'accept' hoặc 'reject'.");
    }
}
