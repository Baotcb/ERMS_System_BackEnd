using ERMS.Application.Features.Geolocation.Queries.GetPublicGeolocation;
using ERMS.Application.Interface;
using FluentAssertions;
using Moq;

namespace ERMS.UnitTests.Features.Geolocation.Queries.GetPublicGeolocation;

public sealed class GetPublicGeolocationHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnResponseFromService()
    {
        var expected = new GetPublicGeolocationResponse
        {
            City = "Ha Noi",
            RegionName = "Ha Noi"
        };

        var geolocationServiceMock = new Mock<IGeolocationService>();
        geolocationServiceMock
            .Setup(service => service.GetCurrentLocationAsync(CancellationToken.None))
            .ReturnsAsync(expected);

        var handler = new GetPublicGeolocationHandler(geolocationServiceMock.Object);

        var result = await handler.Handle(new GetPublicGeolocationQuery(), CancellationToken.None);

        result.Should().BeSameAs(expected);
        geolocationServiceMock.Verify(
            service => service.GetCurrentLocationAsync(CancellationToken.None),
            Times.Once);
    }
}
