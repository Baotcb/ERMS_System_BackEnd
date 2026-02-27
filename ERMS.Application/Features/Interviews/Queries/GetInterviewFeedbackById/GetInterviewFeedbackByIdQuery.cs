using System;
using MediatR;

namespace ERMS.Application.Features.Interviews.Queries.GetInterviewFeedbackById;

/// <summary>
/// Query for Department Head to retrieve detailed feedback for a specific completed interview.
/// </summary>
public sealed class GetInterviewFeedbackByIdQuery : IRequest<GetInterviewFeedbackByIdResponse>
{
    public Guid InterviewId { get; set; }
}
