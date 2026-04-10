using MediatR;

namespace ERMS.Application.Features.Applications.Commands.RejectOffer;

/// <summary>
/// Command for a candidate to reject a job offer.
/// OfferId is passed via the request body, NOT in the URL path.
/// </summary>
public sealed class RejectOfferCommand : IRequest<RejectOfferResult>
{
    /// <summary>
    /// The Offer ID to reject
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// Required note from the candidate explaining the rejection
    /// </summary>
    public string CandidateNote { get; set; } = null!;
}
