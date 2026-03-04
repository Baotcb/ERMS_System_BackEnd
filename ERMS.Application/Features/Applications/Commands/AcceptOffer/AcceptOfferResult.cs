namespace ERMS.Application.Features.Applications.Commands.AcceptOffer;

/// <summary>
/// Result returned after successfully accepting an offer
/// </summary>
public sealed class AcceptOfferResult
{
    public Guid OfferId { get; set; }
    public string NewOfferStatus { get; set; } = null!;
    public string ApplicationStage { get; set; } = null!;
    public DateTime RespondedAt { get; set; }
}
