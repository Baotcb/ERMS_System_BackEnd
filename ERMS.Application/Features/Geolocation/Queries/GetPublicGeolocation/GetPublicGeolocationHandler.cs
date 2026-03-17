using ERMS.Application.Interface;
using MediatR;

namespace ERMS.Application.Features.Geolocation.Queries.GetPublicGeolocation;

public sealed class GetPublicGeolocationHandler
    : IRequestHandler<GetPublicGeolocationQuery, GetPublicGeolocationResponse>
{
    private readonly IGeolocationService _geolocationService;

    public GetPublicGeolocationHandler(IGeolocationService geolocationService)
    {
        _geolocationService = geolocationService;
    }

    public Task<GetPublicGeolocationResponse> Handle(
        GetPublicGeolocationQuery request,
        CancellationToken cancellationToken)
    {
        return _geolocationService.GetCurrentLocationAsync(cancellationToken);
    }
}
