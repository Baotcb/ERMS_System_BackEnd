using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERMS.Application.Interface;

public interface IGoogleCalendarService
{
    Task<string> CreateMeetingAsync(string title, DateTime start, int duration, List<string> attendeeEmails);
}
