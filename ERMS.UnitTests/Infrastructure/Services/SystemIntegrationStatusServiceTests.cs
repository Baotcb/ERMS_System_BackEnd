using ERMS.Application.Interface;
using ERMS.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace ERMS.UnitTests.Infrastructure.Services;

public class SystemIntegrationStatusServiceTests
{
    [Fact]
    public async Task GetSystemIntegrationsAsync_ShouldReturnOnlyGeminiIntegration()
    {
        var aiServiceConfiguration = new Mock<IAIServiceConfiguration>();
        aiServiceConfiguration.SetupGet(x => x.HasApiKey).Returns(true);

        var service = new SystemIntegrationStatusService(aiServiceConfiguration.Object);

        var result = await service.GetSystemIntegrationsAsync(CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Gemini");
        result[0].Category.Should().Be("AI");
        result[0].Status.Should().Be("Configured");
        result[0].EnvironmentScope.Should().Be("System");
        result[0].LastChecked.Should().NotBeNull();
    }
}
