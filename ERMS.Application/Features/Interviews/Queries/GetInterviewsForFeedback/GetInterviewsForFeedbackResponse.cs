using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Interviews.Queries.GetInterviewsForFeedback;

public sealed class GetInterviewsForFeedbackResponse
{
    public List<InterviewFeedbackSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public sealed class InterviewFeedbackSummaryDto
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
    public int RoundNumber { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Feedback aggregation info
    public int FeedbacksReceived { get; set; }
    public int TotalInterviewers { get; set; }
    
    // Decision status
    public string? DepartmentHeadDecision { get; set; }
}
