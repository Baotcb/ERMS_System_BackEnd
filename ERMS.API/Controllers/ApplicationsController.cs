using ERMS.Application.Features.Applications.Commands.SubmitApplication;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers;

/// <summary>
/// Job Applications API - Candidate application submission and management
/// </summary>
[Route("api/applications")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class ApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApplicationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Submit a job application with CV (PDF file)
    /// </summary>
    /// <remarks>
    /// **Process:**
    /// 1. Uploads CV to cloud storage (Cloudinary)
    /// 2. Extracts text from PDF
    /// 3. Uses AI (Gemini) to score CV against job requirements
    /// 4. Creates application record with screening results
    /// 
    /// **Constraints:**
    /// - CV must be PDF format
    /// - Maximum file size: 5MB
    /// - User must have the Candidate role
    /// - Cannot apply to the same job twice
    /// </remarks>
    /// <param name="command">Application data with CV file</param>
    /// <returns>Application ID and CV screening results</returns>
    [HttpPost]
    [Authorize(Roles = AppRoles.Candidate)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SubmitApplicationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Submit([FromForm] SubmitApplicationCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Application submitted successfully.",
                data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
