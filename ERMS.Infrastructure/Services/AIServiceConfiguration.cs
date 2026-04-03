using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ERMS.Infrastructure.Services;

public sealed class AIServiceConfiguration : IAIServiceConfiguration
{
    private readonly GeminiSettings _settings;

    public AIServiceConfiguration(IOptions<GeminiSettings> options)
    {
        _settings = options.Value;
    }

    public string ProviderName => "Gemini";

    public string ModelName => string.IsNullOrWhiteSpace(_settings.Model)
        ? "gemini-2.5-flash"
        : _settings.Model;

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_settings.ApiKey);
}
