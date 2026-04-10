using System.Net;
using System.Text;
using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using ERMS.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ERMS.UnitTests.Infrastructure.Services;

public class CvInfoExtractorServiceTests
{
    [Fact]
    public async Task ExtractContactInfoAsync_ShouldFallbackToPdfPrompt_WhenTextExtractionReturnsEmptyFields()
    {
        // Arrange
        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.7 sample");
        var handler = new SequenceHttpMessageHandler(
            CreateGeminiJsonResponse("""{"fullName":null,"email":null,"phone":null}"""),
            CreateGeminiJsonResponse("""{"fullName":"Hieu Lul","email":"fafac191@gmail.com","phone":"0386708860"}"""));

        var service = CreateService(handler);

        // Act
        var result = await service.ExtractContactInfoAsync("Candidate profile text", pdfBytes, CancellationToken.None);

        // Assert
        result.FullName.Should().Be("Hieu Lul");
        result.Email.Should().Be("fafac191@gmail.com");
        result.Phone.Should().Be("0386708860");

        handler.RequestBodies.Should().HaveCount(2);
        handler.RequestBodies[1].Should().Contain("\"inline_data\"");
        handler.RequestBodies[1].Should().Contain("\"mime_type\":\"application/pdf\"");
        handler.RequestBodies[1].Should().Contain(Convert.ToBase64String(pdfBytes));
    }

    [Fact]
    public async Task ExtractContactInfoAsync_ShouldUsePdfPromptImmediately_WhenResumeTextIsBlank()
    {
        // Arrange
        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.7 sample");
        var handler = new SequenceHttpMessageHandler(
            CreateGeminiJsonResponse("""{"fullName":"Hieu Lul","email":"fafac191@gmail.com","phone":"0386708860"}"""));

        var service = CreateService(handler);

        // Act
        var result = await service.ExtractContactInfoAsync("   ", pdfBytes, CancellationToken.None);

        // Assert
        result.FullName.Should().Be("Hieu Lul");
        result.Email.Should().Be("fafac191@gmail.com");
        result.Phone.Should().Be("0386708860");

        handler.RequestBodies.Should().HaveCount(1);
        handler.RequestBodies[0].Should().Contain("\"inline_data\"");
        handler.RequestBodies[0].Should().Contain("\"mime_type\":\"application/pdf\"");
    }

    private static CvInfoExtractorService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var logger = Mock.Of<ILogger<CvInfoExtractorService>>();
        var options = Options.Create(new GeminiSettings
        {
            ApiKey = "test-key",
            Model = "gemini-2.5-flash"
        });

        return new CvInfoExtractorService(httpClient, logger, options);
    }

    private static HttpResponseMessage CreateGeminiJsonResponse(string jsonPayload)
    {
        var responseJson = $$"""
            {
              "candidates": [
                {
                  "content": {
                    "parts": [
                      {
                        "text": {{JsonEncoded(jsonPayload)}}
                      }
                    ]
                  }
                }
              ]
            }
            """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };
    }

    private static string JsonEncoded(string value)
    {
        return System.Text.Json.JsonSerializer.Serialize(value);
    }

    private sealed class SequenceHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return _responses.Dequeue();
        }
    }
}
