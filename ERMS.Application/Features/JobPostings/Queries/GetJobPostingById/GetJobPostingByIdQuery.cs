using MediatR;

namespace ERMS.Application.Features.JobPostings.Queries.GetJobPostingById;

public sealed class GetJobPostingByIdQuery : IRequest<JobPostingDetailDto?>
{
    public Guid Id { get; set; }
}
