using ERMS.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace ERMS.UnitTests.Infrastructure.Services;

public class EmailServiceTests
{
    [Fact]
    public void GetEmailSettings_ShouldNormalizePassword_ForGmail()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:SmtpServer"] = "smtp.gmail.com",
                ["EmailSettings:SenderName"] = "ERMS",
                ["EmailSettings:SenderEmail"] = "erms2026@gmail.com",
                ["EmailSettings:Port"] = "587",
                ["EmailSettings:Password"] = "ab cd  ef gh"
            })
            .Build();

        var service = new EmailService(config);

        // Act
        var settings = InvokeGetEmailSettings(service);
        var password = settings.GetType().GetProperty("Password", BindingFlags.Instance | BindingFlags.Public)!.GetValue(settings) as string;

        // Assert
        password.Should().Be("abcdefgh");
    }

    [Fact]
    public void GetEmailSettings_ShouldKeepPassword_ForNonGmail()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:SmtpServer"] = "smtp.mailtrap.io",
                ["EmailSettings:SenderName"] = "ERMS",
                ["EmailSettings:SenderEmail"] = "test@example.com",
                ["EmailSettings:Port"] = "587",
                ["EmailSettings:Password"] = "  keep spaces  "
            })
            .Build();

        var service = new EmailService(config);

        // Act
        var settings = InvokeGetEmailSettings(service);
        var password = settings.GetType().GetProperty("Password", BindingFlags.Instance | BindingFlags.Public)!.GetValue(settings) as string;

        // Assert
        password.Should().Be("keep spaces");
    }

    private static object InvokeGetEmailSettings(EmailService service)
    {
        var method = typeof(EmailService).GetMethod("GetEmailSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var result = method!.Invoke(service, null);
        result.Should().NotBeNull();
        return result!;
    }
}
