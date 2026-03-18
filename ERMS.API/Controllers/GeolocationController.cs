using ERMS.Application.Features.Geolocation.Queries.GetPublicGeolocation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers;

/// <summary>
/// Public geolocation endpoint for frontend location detection.
/// </summary>
[Route("api/public/geolocation")]
[ApiController]
[Produces("application/json")]
public class GeolocationController : ControllerBase
{
    private readonly IMediator _mediator;

    public GeolocationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Detect the current user location from the request IP.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetPublicGeolocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLocation()
    {
        try
        {
            var result = await _mediator.Send(new GetPublicGeolocationQuery());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
