namespace ERMS.Application.Interface;

public sealed class SkillGapChatRequestDto
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class SkillGapChatResponseDto
{
    public Guid ConversationId { get; set; }
    public string AssistantMessage { get; set; } = string.Empty;
    public List<SkillGapChatSuggestionDto> Suggestions { get; set; } = [];
}

public sealed class SkillGapChatSuggestionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? SkillName { get; set; }
}
