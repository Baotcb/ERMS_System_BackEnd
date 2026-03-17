using System.Net;
using System.Text;
using ERMS.Application.Features.Geolocation.Queries.GetPublicGeolocation;
using ERMS.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace ERMS.UnitTests.Services;

public sealed class GeolocationServiceTests
{
    [Fact]
    public async Task GetCurrentLocationAsync_ShouldCacheSuccessfulResponse()
    {
        var messageHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","city":"Ha Noi","regionName":"Ha Noi"}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var service = CreateService(
            messageHandler,
            remoteIpAddress: "203.0.113.10");

        var firstResult = await service.GetCurrentLocationAsync(CancellationToken.None);
        var secondResult = await service.GetCurrentLocationAsync(CancellationToken.None);

        firstResult.Should().BeEquivalentTo(new GetPublicGeolocationResponse
        {
            City = "Ha Noi",
            RegionName = "Ha Noi"
        });
        secondResult.Should().BeEquivalentTo(firstResult);
        messageHandler.RequestUris.Should().ContainSingle();
        messageHandler.RequestUris[0].Should()
            .Be(new Uri("http://ip-api.com/json/203.0.113.10?fields=status,city,regionName&lang=vi"));
    }

    [Fact]
    public async Task GetCurrentLocationAsync_ShouldReturnEmptyResponse_WhenProviderFails()
    {
        var messageHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"fail"}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var service = CreateService(
            messageHandler,
            remoteIpAddress: "198.51.100.20");

        var result = await service.GetCurrentLocationAsync(CancellationToken.None);

        result.City.Should().BeNull();
        result.RegionName.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentLocationAsync_ShouldIgnoreRawForwardedHeader_WhenRemoteIpIsDifferent()
    {
        var messageHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","city":"Da Nang","regionName":"Da Nang"}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var service = CreateService(
            messageHandler,
            remoteIpAddress: "198.51.100.20",
            forwardedFor: "203.0.113.99");

        await service.GetCurrentLocationAsync(CancellationToken.None);

        messageHandler.RequestUris.Should().ContainSingle();
        messageHandler.RequestUris[0].Should()
            .Be(new Uri("http://ip-api.com/json/198.51.100.20?fields=status,city,regionName&lang=vi"));
    }

    private static GeolocationService CreateService(
        HttpMessageHandler handler,
        string remoteIpAddress,
        string? forwardedFor = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://ip-api.com/")
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(remoteIpAddress);

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            httpContext.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(accessor => accessor.HttpContext).Returns(httpContext);

        return new GeolocationService(
            httpClient,
            httpContextAccessorMock.Object,
            new MemoryCache(new MemoryCacheOptions()));
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);
            return Task.FromResult(_responseFactory(request));
        }
    }
}
