using MediatR;

namespace ERMS.Application.Features.JobPostings.Commands.PublishJobPosting;

public sealed class PublishJobPostingCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}
