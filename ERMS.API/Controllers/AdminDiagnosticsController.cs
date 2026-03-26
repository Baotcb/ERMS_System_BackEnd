using ERMS.Application.Features.Diagnostics.Queries.GetBackendEgressIp;
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
    private readonly IBackendEgressIpService _backendEgressIpService;

    public AdminDiagnosticsController(IBackendEgressIpService backendEgressIpService)
    {
        _backendEgressIpService = backendEgressIpService;
    }

    /// <summary>
    /// Returns the current public egress IP that the backend uses for outbound HTTP requests.
    /// </summary>
    [HttpGet("backend-egress-ip")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(GetBackendEgressIpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBackendEgressIp(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _backendEgressIpService.GetPublicEgressIpAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
