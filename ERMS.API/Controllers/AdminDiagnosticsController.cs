using ERMS.Application.Features.Diagnostics.Queries.GetGeminiProbe;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERMS.API.Controllers;

/// <summary>
/// Temporary admin-only diagnostics endpoints for backend infrastructure checks.
/// </summary>
[Route("api/admin")]
[ApiController]
[EnableRateLimiting("fixed")]
public sealed class AdminDiagnosticsController : ControllerBase
{
    private readonly IAIProbeService _aiProbeService;

    public AdminDiagnosticsController(IAIProbeService aiProbeService)
    {
        _aiProbeService = aiProbeService;
    }

    /// <summary>
    /// Calls Gemini directly from the backend and returns the raw connectivity result.
    /// </summary>
    [HttpGet("gemini-probe")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(GetGeminiProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGeminiProbe(CancellationToken cancellationToken)
    {
        var result = await _aiProbeService.ProbeAsync(cancellationToken);
        return Ok(result);
    }
}
