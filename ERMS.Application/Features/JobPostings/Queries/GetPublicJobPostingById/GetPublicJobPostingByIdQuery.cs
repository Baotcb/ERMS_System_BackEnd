using MediatR;

namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostingById;

/// <summary>
/// Query to get a single published job posting by ID (public access)
/// </summary>
public sealed class GetPublicJobPostingByIdQuery : IRequest<PublicJobPostingDetailDto?>
{
    public Guid Id { get; set; }
}
