using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;
using ERMS.Application.Interface;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Queries.GetSystemIntegrations;

public class GetSystemIntegrationsHandlerTests
{
    private readonly Mock<ISystemIntegrationStatusService> _integrationStatusService;
    private readonly GetSystemIntegrationsHandler _handler;

    public GetSystemIntegrationsHandlerTests()
    {
        _integrationStatusService = new Mock<ISystemIntegrationStatusService>();
        _handler = new GetSystemIntegrationsHandler(_integrationStatusService.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnGeminiIntegrationMetadata()
    {
        var checkedAt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        _integrationStatusService
            .Setup(service => service.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSystemIntegrationsResponse
            {
                new() { Name = "Gemini", Category = "AI", Status = "Configured", EnvironmentScope = "System", LastChecked = checkedAt }
            });

        var result = await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(
            [
                new
                {
                    Name = "Gemini",
                    Category = "AI",
                    Status = "Configured",
                    EnvironmentScope = "System",
                    LastChecked = (DateTime?)checkedAt
                }
            ],
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Handle_ShouldExposeOnlyApprovedSafeFields()
    {
        _integrationStatusService
            .Setup(service => service.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSystemIntegrationsResponse
            {
                new() { Name = "Gemini", Category = "AI", Status = "Configured", EnvironmentScope = "System", LastChecked = DateTime.UtcNow }
            });

        var result = await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        typeof(SystemIntegrationDto).GetProperties()
            .Select(property => property.Name)
            .Should()
            .BeEquivalentTo(["Name", "Category", "Status", "EnvironmentScope", "LastChecked"]);

        result.Should().OnlyContain(integration =>
            !integration.Name.Contains("secret", StringComparison.OrdinalIgnoreCase) &&
            !integration.Category.Contains("secret", StringComparison.OrdinalIgnoreCase) &&
            !integration.Status.Contains("key", StringComparison.OrdinalIgnoreCase) &&
            !integration.Status.Contains("token", StringComparison.OrdinalIgnoreCase) &&
            !integration.EnvironmentScope.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_ShouldReturnSingleGeminiIntegration()
    {
        _integrationStatusService
            .Setup(service => service.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSystemIntegrationsResponse
            {
                new() { Name = "Gemini", Category = "AI", Status = "Configured", EnvironmentScope = "System", LastChecked = DateTime.UtcNow }
            });

        var result = await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result.Select(integration => integration.Name).Should().ContainSingle("Gemini");
    }

    [Fact]
    public async Task Handle_ShouldIncludeGeminiAsAiIntegration_WithSystemScope()
    {
        _integrationStatusService
            .Setup(service => service.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSystemIntegrationsResponse
            {
                new() { Name = "Gemini", Category = "AI", Status = "Configured", EnvironmentScope = "System", LastChecked = DateTime.UtcNow }
            });

        var result = await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        result.Should().ContainSingle(integration =>
            integration.Name == "Gemini" &&
            integration.Category == "AI" &&
            integration.EnvironmentScope == "System");
    }

    [Fact]
    public async Task Handle_ShouldNotIncludeLegacyNonAiIntegrations()
    {
        _integrationStatusService
            .Setup(service => service.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSystemIntegrationsResponse
            {
                new() { Name = "Gemini", Category = "AI", Status = "Configured", EnvironmentScope = "System", LastChecked = DateTime.UtcNow }
            });

        var result = await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        result.Should().NotContain(integration => integration.Name == "Google OAuth");
        result.Should().NotContain(integration => integration.Name == "SMTP");
        result.Should().NotContain(integration => integration.Name == "Cloudinary");
        result.Should().NotContain(integration => integration.Name == "Zoom");
        result.Should().NotContain(integration => integration.Name == "Geolocation");
    }

    [Fact]
    public async Task Handle_ShouldStampLastChecked_WhenReportingIntegrationStatus()
    {
        _integrationStatusService
            .Setup(service => service.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSystemIntegrationsResponse
            {
                new() { Name = "Gemini", Category = "AI", Status = "Configured", EnvironmentScope = "System", LastChecked = DateTime.UtcNow }
            });

        var result = await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        result.Should().OnlyContain(integration => integration.LastChecked.HasValue);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToIntegrationStatusService()
    {
        _integrationStatusService
            .Setup(service => service.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSystemIntegrationsResponse());

        await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        _integrationStatusService.Verify(
            service => service.GetSystemIntegrationsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
