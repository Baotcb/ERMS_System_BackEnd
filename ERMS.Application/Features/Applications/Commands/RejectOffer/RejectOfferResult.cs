namespace ERMS.Application.Features.Applications.Commands.RejectOffer;

/// <summary>
/// Result returned after successfully rejecting an offer
/// </summary>
public sealed class RejectOfferResult
{
    public Guid OfferId { get; set; }
    public string NewOfferStatus { get; set; } = null!;
    public DateTime RespondedAt { get; set; }
}
