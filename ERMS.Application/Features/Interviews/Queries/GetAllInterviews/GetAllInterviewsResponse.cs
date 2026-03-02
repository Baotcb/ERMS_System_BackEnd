using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Interviews.Queries.GetAllInterviews;

/// <summary>
/// Response object containing a paginated list of comprehensive interview details for HR.
/// </summary>
public sealed class GetAllInterviewsResponse
{
    public List<InterviewDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public sealed class InterviewDto
{
    public Guid InterviewId { get; set; }
    public Guid ApplicationId { get; set; }
    
    // Candidate details
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    
    // Job details
    public string JobTitle { get; set; } = string.Empty;
    
    // Interview details
    public string InterviewType { get; set; } = string.Empty;
    public string InterviewFormat { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public int Duration { get; set; }
    public string? Location { get; set; }
    public string? MeetingLink { get; set; }
    public string Status { get; set; } = string.Empty;
    
    // Assigner info
    public Guid ScheduledById { get; set; }
    public string ScheduledByName { get; set; } = string.Empty;
    
    // Participant summary
    public List<InterviewParticipantSummaryDto> Participants { get; set; } = new();
}

public sealed class InterviewParticipantSummaryDto
{
    public Guid ParticipantId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ConfirmationStatus { get; set; } = string.Empty;
    public bool HasSubmittedFeedback { get; set; }
}
