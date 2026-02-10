using ERMS.Application.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERMS.Infrastructure.Services;

public class MockGoogleCalendarService : IGoogleCalendarService
{
    public Task<string> CreateMeetingAsync(string title, DateTime start, int duration, List<string> attendeeEmails)
    {
        // Return a dummy link as per requirements
        return Task.FromResult("https://meet.google.com/mock-test-link-123");
    }
}
