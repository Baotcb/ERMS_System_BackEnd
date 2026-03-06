using MediatR;

namespace ERMS.Application.Features.JobPostings.Commands.UnsaveJobPosting;

/// <summary>
/// Command for a candidate to unsave a job posting.
/// JobPostingId is passed in the request body.
/// </summary>
public sealed class UnsaveJobPostingCommand : IRequest<UnsaveJobPostingResult>
{
    public Guid JobPostingId { get; set; }
}
