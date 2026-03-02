using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Interviews.Queries.GetInterviewFeedbackById;

public sealed class GetInterviewFeedbackByIdResponse
{
    public Guid InterviewId { get; set; }
    public Guid ApplicationId { get; set; }
    
    // Core Candidate & Job Info
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    
    // Interview Context
    public string InterviewType { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Final Decision details (if already made by Department Head)
    public string? DepartmentHeadDecision { get; set; }
    public int? DepartmentHeadOverallRating { get; set; }
    public string? DepartmentHeadOverallFeedback { get; set; }
    public string? DepartmentHeadNote { get; set; }
    
    // Individual Interviewer Feedbacks
    public List<ParticipantFeedbackDto> ParticipantsFeedback { get; set; } = new();
}

public sealed class ParticipantFeedbackDto
{
    public Guid ParticipantId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    
    // Their individual feedback submission
    public int? Rating { get; set; }
    public string? Feedback { get; set; }
    public string? Recommendation { get; set; }
    public DateTime? FeedbackSubmittedAt { get; set; }
}
