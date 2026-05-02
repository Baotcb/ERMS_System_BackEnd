using MediatR;

namespace ERMS.Application.Features.Training.Commands.DeleteChatConversation;

public sealed class DeleteChatConversationCommand : IRequest<bool>
{
    public Guid ConversationId { get; set; }
}
