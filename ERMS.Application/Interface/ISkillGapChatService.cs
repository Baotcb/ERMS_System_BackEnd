namespace ERMS.Application.Interface;

public interface ISkillGapChatService
{
    Task<SkillGapChatResponseDto> AskAsync(
        SkillGapChatRequestDto request,
        CancellationToken cancellationToken = default);
}
