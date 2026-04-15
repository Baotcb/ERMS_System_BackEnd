using MediatR;

namespace ERMS.Application.Features.Applications.Queries.GetOfferByToken;

public sealed class GetOfferByTokenQuery : IRequest<GetOfferByTokenResult>
{
    public string Token { get; set; } = string.Empty;
}
