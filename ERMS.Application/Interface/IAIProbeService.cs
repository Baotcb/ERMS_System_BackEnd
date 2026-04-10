using ERMS.Application.Features.Diagnostics.Queries.GetGeminiProbe;

namespace ERMS.Application.Interface;

/// <summary>
/// Provider-agnostic AI connectivity probe interface.
/// </summary>
public interface IAIProbeService
{
    Task<GetGeminiProbeResponse> ProbeAsync(CancellationToken cancellationToken);
}
