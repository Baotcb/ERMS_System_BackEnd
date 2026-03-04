namespace ERMS.Application.Interface;

public interface ICalendarService
{
    string CreateICalendarEvent(CalendarEventRequest request);
}

public record CalendarEventRequest
{
    public string Subject { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public int DurationMinutes { get; init; }
    public List<string> AttendeeEmails { get; init; } = new();
    public string OrganizerEmail { get; init; } = string.Empty;
    public string OrganizerName { get; init; } = string.Empty;
}