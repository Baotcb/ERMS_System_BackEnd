using FluentValidation;

namespace ERMS.Application.Features.Training.Commands.SendChatMessage;

public sealed class SendChatMessageValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
