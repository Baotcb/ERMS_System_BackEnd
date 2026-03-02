namespace ERMS.Application.Interface;

public interface IZoomService
{
    Task<ZoomMeetingResponse> CreateMeetingAsync(ZoomMeetingRequest request, CancellationToken cancellationToken = default);
}

public record ZoomMeetingRequest
{
    public string Topic { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public int Duration { get; init; }
    public string Timezone { get; init; } = "UTC";
    public string Agenda { get; init; } = string.Empty;
}

public record ZoomMeetingResponse
{
    public string MeetingId { get; init; } = string.Empty;
    public string JoinUrl { get; init; } = string.Empty;
    public string StartUrl { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}