using MediatR;

namespace ERMS.Application.Features.Training.Queries.GetChatHistory;

public sealed class GetChatHistoryQuery : IRequest<GetChatHistoryResult>
{
    public Guid ConversationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class GetChatHistoryResult
{
    public List<ChatHistoryItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int TotalPages =>
        PageSize <= 0 ? 0 :
        (int)Math.Ceiling((double)TotalCount / PageSize);
}

public sealed class ChatHistoryItemDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
