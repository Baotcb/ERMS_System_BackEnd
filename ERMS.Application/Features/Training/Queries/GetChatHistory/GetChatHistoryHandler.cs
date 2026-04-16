using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Training.Queries.GetChatHistory;

public sealed class GetChatHistoryHandler
    : IRequestHandler<GetChatHistoryQuery, GetChatHistoryResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetChatHistoryHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetChatHistoryResult> Handle(
        GetChatHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var conversationExists = await _context.ChatConversations
            .AnyAsync(
                c =>
                    c.Id == request.ConversationId &&
                    c.EnterpriseId == enterpriseId &&
                    c.UserId == userId &&
                    !c.IsDeleted,
                cancellationToken);

        if (!conversationExists)
        {
            throw new Exception("Không tìm thấy cuộc trò chuyện.");
        }

        var query = _context.ChatMessages
            .Where(m =>
                m.ChatConversationId == request.ConversationId &&
                m.EnterpriseId == enterpriseId &&
                !m.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ChatHistoryItemDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetChatHistoryResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
