namespace ERMS.Application.Features.Applications.Commands.RespondOfferByToken;

public sealed class RespondOfferByTokenResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string? OfferStatus { get; set; }
}
