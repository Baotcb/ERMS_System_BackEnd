using MediatR;

namespace ERMS.Application.Features.JobPostings.Commands.DeleteJobPosting;

public sealed class DeleteJobPostingCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}
