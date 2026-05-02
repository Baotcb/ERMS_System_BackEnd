namespace ERMS.Application.Features.Applications.Commands.RespondOfferByToken;

public sealed class RespondOfferByTokenResult
{
    public Guid OfferId { get; set; }
    public string NewOfferStatus { get; set; } = string.Empty;
    public DateTime RespondedAt { get; set; }
}
