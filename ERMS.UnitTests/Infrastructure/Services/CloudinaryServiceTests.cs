using ERMS.Infrastructure.Configuration;
using ERMS.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ERMS.UnitTests.Infrastructure.Services;

public class CloudinaryServiceTests
{
    [Fact]
    public void GetAuthenticatedDownloadUrl_ShouldCreateSignedRawDownloadUrl_FromStoredResumeUrl()
    {
        // Arrange
        var service = new CloudinaryService(
            NullLogger<CloudinaryService>.Instance,
            Options.Create(new CloudinarySettings
            {
                CloudName = "demo-cloud",
                ApiKey = "test-key",
                ApiSecret = "test-secret"
            }));

        const string fileUrl = "https://res.cloudinary.com/demo-cloud/raw/upload/v1775814433/erms/resumes/1775814434_Hieu_Lul_TopCV.vn_240326.135248.pdf";

        // Act
        var result = service.GetAuthenticatedDownloadUrl(
            fileUrl,
            "Hieu_Lul_TopCV.vn_240326.135248.pdf",
            TimeSpan.FromMinutes(5));

        // Assert
        var uri = new Uri(result);

        uri.Scheme.Should().Be(Uri.UriSchemeHttps);
        uri.Host.Should().Be("api.cloudinary.com");
        uri.AbsolutePath.Should().Be("/v1_1/demo-cloud/raw/download");
        uri.Query.Should().Contain("public_id=erms/resumes/1775814434_Hieu_Lul_TopCV.vn_240326.135248.pdf");
        uri.Query.Should().NotContain("format=");
        uri.Query.Should().Contain("api_key=test-key");
        uri.Query.Should().Contain("signature=");
        uri.Query.Should().Contain("attachment=");
        uri.Query.Should().Contain("type=upload");
    }
}
