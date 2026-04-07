using FluentValidation;

namespace ERMS.Application.Features.Subscription.Commands.CreatePaymentOrder;

public class CreatePaymentOrderValidator : AbstractValidator<CreatePaymentOrderCommand>
{
    public CreatePaymentOrderValidator()
    {
        RuleFor(x => x.SubscriptionPlanId)
            .NotEmpty()
            .WithMessage("SubscriptionPlanId is required");
    }
}
