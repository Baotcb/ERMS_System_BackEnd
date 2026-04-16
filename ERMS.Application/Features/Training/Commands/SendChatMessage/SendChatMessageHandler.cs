using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;

namespace ERMS.Application.Features.Training.Commands.SendChatMessage;

public sealed class SendChatMessageHandler
    : IRequestHandler<SendChatMessageCommand, SkillGapChatResponseDto>
{
    private readonly ISkillGapChatService _chatService;
    private readonly ICurrentUserService _currentUserService;

    public SendChatMessageHandler(
        ISkillGapChatService chatService,
        ICurrentUserService currentUserService)
    {
        _chatService = chatService;
        _currentUserService = currentUserService;
    }

    public async Task<SkillGapChatResponseDto> Handle(
        SendChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        _ = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");

        _ = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        var roles = _currentUserService.Roles ?? [];
        if (!roles.Contains(AppRoles.DepartmentHead))
        {
            throw new UnauthorizedAccessException("Chỉ DepartmentHead mới được phép sử dụng trợ lý skill gap.");
        }

        return await _chatService.AskAsync(
            new SkillGapChatRequestDto
            {
                ConversationId = request.ConversationId,
                Message = request.Message.Trim()
            },
            cancellationToken);
    }
}
