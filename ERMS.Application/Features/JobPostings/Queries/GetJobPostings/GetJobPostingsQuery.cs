using MediatR;

namespace ERMS.Application.Features.JobPostings.Queries.GetJobPostings;

public sealed class GetJobPostingsQuery : IRequest<GetJobPostingsResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }
}
