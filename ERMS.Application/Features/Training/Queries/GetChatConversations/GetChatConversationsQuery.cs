using MediatR;

namespace ERMS.Application.Features.Training.Queries.GetChatConversations;

public sealed class GetChatConversationsQuery : IRequest<GetChatConversationsResult>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class GetChatConversationsResult
{
    public List<ChatConversationItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int TotalPages =>
        PageSize <= 0 ? 0 :
        (int)Math.Ceiling((double)TotalCount / PageSize);
}

public sealed class ChatConversationItemDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
