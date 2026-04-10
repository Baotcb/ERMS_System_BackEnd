using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ERMS.Infrastructure.Services;

public sealed class GroqAIServiceConfiguration : IAIServiceConfiguration
{
    private readonly GroqSettings _settings;

    public GroqAIServiceConfiguration(IOptions<GroqSettings> options)
    {
        _settings = options.Value;
    }

    public string ProviderName => "Groq";

    public string ModelName => string.IsNullOrWhiteSpace(_settings.Model)
        ? "llama-3.3-70b-versatile"
        : _settings.Model;

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_settings.ApiKey);
}
