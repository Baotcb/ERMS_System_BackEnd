namespace ERMS.Application.Features.Applications.Commands.CancelOffer;

/// <summary>
/// Result returned after successfully cancelling an offer
/// </summary>
public sealed class CancelOfferResult
{
    public Guid OfferId { get; set; }
    public string NewOfferStatus { get; set; } = null!;
    public string NewApplicationStage { get; set; } = null!;
    public DateTime CancelledAt { get; set; }
}
