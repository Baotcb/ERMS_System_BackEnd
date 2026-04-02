using ERMS.Application.Features.Diagnostics.Queries.GetGeminiProbe;

namespace ERMS.Application.Interface;

public interface IGeminiProbeService
{
    Task<GetGeminiProbeResponse> ProbeAsync(CancellationToken cancellationToken);
}
