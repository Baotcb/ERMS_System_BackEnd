using MediatR;

namespace ERMS.Application.Features.JobPostings.Commands.CloseJobPosting;

public sealed class CloseJobPostingCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}
