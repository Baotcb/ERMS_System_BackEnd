using ERMS.Application.Interface;
using MediatR;

namespace ERMS.Application.Features.Training.Commands.SendChatMessage;

public sealed class SendChatMessageCommand : IRequest<SkillGapChatResponseDto>
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
}
