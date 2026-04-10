using MediatR;

namespace ERMS.Application.Features.Applications.Commands.RespondOfferByToken;

public sealed class RespondOfferByTokenCommand : IRequest<RespondOfferByTokenResult>
{
    public Guid Token { get; set; }
    /// <summary>"accept" or "reject"</summary>
    public string Action { get; set; } = null!;
}
