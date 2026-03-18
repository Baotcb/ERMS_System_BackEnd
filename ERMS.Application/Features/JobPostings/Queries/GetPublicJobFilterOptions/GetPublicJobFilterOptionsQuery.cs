using MediatR;

namespace ERMS.Application.Features.JobPostings.Queries.GetPublicJobFilterOptions;

public sealed record GetPublicJobFilterOptionsQuery : IRequest<GetPublicJobFilterOptionsResponse>;
