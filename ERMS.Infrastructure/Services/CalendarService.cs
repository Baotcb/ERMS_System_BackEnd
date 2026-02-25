using ERMS.Application.Interface;
using System.Text;

namespace ERMS.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    public string CreateICalendarEvent(CalendarEventRequest request)
    {
        var icsBuilder = new StringBuilder();
        var now = DateTime.UtcNow;
        var endTime = request.StartTime.AddMinutes(request.DurationMinutes);

        icsBuilder.AppendLine("BEGIN:VCALENDAR");
        icsBuilder.AppendLine("VERSION:2.0");
        icsBuilder.AppendLine("PRODID:-//ERMS//Interview Scheduler//EN");
        icsBuilder.AppendLine("METHOD:REQUEST");
        icsBuilder.AppendLine("BEGIN:VEVENT");
        icsBuilder.AppendLine($"UID:{Guid.NewGuid()}@erms.com");
        icsBuilder.AppendLine($"DTSTAMP:{FormatDateTime(now)}");
        icsBuilder.AppendLine($"DTSTART:{FormatDateTime(request.StartTime)}");
        icsBuilder.AppendLine($"DTEND:{FormatDateTime(endTime)}");
        icsBuilder.AppendLine($"SUMMARY:{EscapeString(request.Subject)}");
        icsBuilder.AppendLine($"DESCRIPTION:{EscapeString(request.Description)}");
        icsBuilder.AppendLine($"LOCATION:{EscapeString(request.Location)}");
        icsBuilder.AppendLine($"ORGANIZER;CN={EscapeString(request.OrganizerName)}:mailto:{request.OrganizerEmail}");

        foreach (var attendee in request.AttendeeEmails)
        {
            icsBuilder.AppendLine($"ATTENDEE;ROLE=REQ-PARTICIPANT;PARTSTAT=NEEDS-ACTION;RSVP=TRUE:mailto:{attendee}");
        }

        icsBuilder.AppendLine("STATUS:CONFIRMED");
        icsBuilder.AppendLine("SEQUENCE:0");
        icsBuilder.AppendLine("BEGIN:VALARM");
        icsBuilder.AppendLine("TRIGGER:-PT15M");
        icsBuilder.AppendLine("ACTION:DISPLAY");
        icsBuilder.AppendLine("DESCRIPTION:Reminder");
        icsBuilder.AppendLine("END:VALARM");
        icsBuilder.AppendLine("END:VEVENT");
        icsBuilder.AppendLine("END:VCALENDAR");

        return icsBuilder.ToString();
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
    }

    private static string EscapeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        return input
            .Replace("\\", "\\\\")
            .Replace(",", "\\,")
            .Replace(";", "\\;")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }
}