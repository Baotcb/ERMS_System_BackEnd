using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ERMS.Infrastructure.Services;

public sealed class GroqAIServiceConfiguration : IAIServiceConfiguration
{
    private readonly GroqSettings _settings;
    private readonly GroqModelSettings _modelSettings;

    public GroqAIServiceConfiguration(IOptions<GroqSettings> options, IOptions<GroqModelSettings> modelOptions)
    {
        _settings = options.Value;
        _modelSettings = modelOptions.Value;
    }

    public string ProviderName => "Groq";

    public string ModelName => ResolveModel();

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    private string ResolveModel()
    {
        if (!string.IsNullOrWhiteSpace(_modelSettings.CvScoring))
        {
            return _modelSettings.CvScoring.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_modelSettings.JobDescription))
        {
            return _modelSettings.JobDescription.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_modelSettings.Probe))
        {
            return _modelSettings.Probe.Trim();
        }

        return "Not configured";
    }
}
