using MediatR;

namespace ERMS.Application.Features.Applications.Commands.RespondOfferByToken;

public sealed class RespondOfferByTokenCommand : IRequest<RespondOfferByTokenResult>
{
    public string Token { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
