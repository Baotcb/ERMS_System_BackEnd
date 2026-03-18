using ERMS.Application.Features.Geolocation.Queries.GetPublicGeolocation;

namespace ERMS.Application.Interface;

public interface IGeolocationService
{
    Task<GetPublicGeolocationResponse> GetCurrentLocationAsync(CancellationToken cancellationToken);
}
