namespace ERMS.Application.Interface;

public interface IAIServiceConfiguration
{
    string ProviderName { get; }
    string ModelName { get; }
    bool HasApiKey { get; }
}
