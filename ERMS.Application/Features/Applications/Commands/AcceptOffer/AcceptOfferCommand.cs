using MediatR;

namespace ERMS.Application.Features.Applications.Commands.AcceptOffer;

/// <summary>
/// Command for a candidate to accept a job offer.
/// OfferId is passed via the request body, NOT in the URL path.
/// </summary>
public sealed class AcceptOfferCommand : IRequest<AcceptOfferResult>
{
    /// <summary>
    /// The Offer ID to accept
    /// </summary>
    public Guid OfferId { get; set; }
}
