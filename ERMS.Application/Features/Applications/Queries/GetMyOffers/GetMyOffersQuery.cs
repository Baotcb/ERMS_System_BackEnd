using MediatR;

namespace ERMS.Application.Features.Applications.Queries.GetMyOffers;

/// <summary>
/// Query for a candidate to retrieve their own offers
/// </summary>
public sealed class GetMyOffersQuery : IRequest<GetMyOffersResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
