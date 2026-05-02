using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Training.Commands.DeleteChatConversation;

public sealed class DeleteChatConversationHandler
    : IRequestHandler<DeleteChatConversationCommand, bool>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteChatConversationHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(
        DeleteChatConversationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(
                c =>
                    c.Id == request.ConversationId &&
                    c.EnterpriseId == enterpriseId &&
                    c.UserId == userId &&
                    !c.IsDeleted,
                cancellationToken);

        if (conversation == null)
        {
            throw new Exception("Không tìm thấy cuộc trò chuyện.");
        }

        conversation.IsDeleted = true;
        conversation.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
