using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;
using FluentAssertions;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Queries.GetSystemIntegrations;

public class GetSystemIntegrationsHandlerTests
{
    private readonly GetSystemIntegrationsHandler _handler;

    public GetSystemIntegrationsHandlerTests()
    {
        _handler = new GetSystemIntegrationsHandler();
    }

    [Fact]
    public async Task Handle_ShouldReturnExpectedSafeConnectorMetadata()
    {
        var result = await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(
            [
                new
                {
                    Name = "Google OAuth",
                    Category = "Authentication",
                    Status = "Configured",
                    EnvironmentScope = "System",
                    LastChecked = (DateTime?)null
                },
                new
                {
                    Name = "SMTP",
                    Category = "Communication",
                    Status = "Configured",
                    EnvironmentScope = "System",
                    LastChecked = (DateTime?)null
                },
                new
                {
                    Name = "Cloudinary",
                    Category = "Media",
                    Status = "Configured",
                    EnvironmentScope = "System",
                    LastChecked = (DateTime?)null
                },
                new
                {
                    Name = "Zoom",
                    Category = "Meeting",
                    Status = "Configured",
                    EnvironmentScope = "System",
                    LastChecked = (DateTime?)null
                },
                new
                {
                    Name = "Gemini",
                    Category = "AI",
                    Status = "Configured",
                    EnvironmentScope = "System",
                    LastChecked = (DateTime?)null
                },
                new
                {
                    Name = "Geolocation",
                    Category = "Location",
                    Status = "Configured",
                    EnvironmentScope = "System",
                    LastChecked = (DateTime?)null
                }
            ],
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Handle_ShouldExposeOnlyApprovedSafeFields()
    {
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
    public async Task Handle_ShouldReturnExactlySixUniqueIntegrations()
    {
        var result = await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        result.Should().HaveCount(6);
        result.Select(integration => integration.Name).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_ShouldIncludeGeminiAsAiIntegration_WithSystemScope()
    {
        var result = await _handler.Handle(new GetSystemIntegrationsQuery(), CancellationToken.None);

        result.Should().ContainSingle(integration =>
            integration.Name == "Gemini" &&
            integration.Category == "AI" &&
            integration.EnvironmentScope == "System");
    }
}
