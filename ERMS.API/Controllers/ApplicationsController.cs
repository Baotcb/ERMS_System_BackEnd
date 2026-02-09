using ERMS.Application.Features.Applications.Commands.SubmitApplication;
using ERMS.Application.Features.Applications.Commands.ForwardApplication;
using ERMS.Application.Features.Applications.Commands.ScheduleInterview;
using ERMS.Application.Features.Applications.Queries.GetApplicationsByJob;
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
    /// Get applications for a specific job posting, sorted by AI match score (highest first)
    /// </summary>
    /// <remarks>
    /// **Access:** HR Manager, Director
    /// 
    /// Returns applications sorted by CVScreeningResult.OverallScore descending to prioritize top AI-matched talent.
    /// Includes candidate info, resume link, and AI screening summary.
    /// </remarks>
    /// <param name="jobPostingId">The job posting ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="stageFilter">Optional filter by stage (e.g., "Applied", "Shortlisted")</param>
    /// <returns>Paginated list of applications</returns>
    [HttpGet("job/{jobPostingId}")]
    [Authorize(Roles = $"{AppRoles.HRManager},{AppRoles.Director}")]
    [ProducesResponseType(typeof(GetApplicationsByJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByJob(
        Guid jobPostingId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? stageFilter = null)
    {
        try
        {
            var query = new GetApplicationsByJobQuery
            {
                JobPostingId = jobPostingId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                StageFilter = stageFilter
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Forward (shortlist) an application - changes stage from Applied to Shortlisted
    /// </summary>
    /// <remarks>
    /// **Access:** HR Manager only
    /// 
    /// This action forwards a candidate to the Department Head for further review.
    /// The application must be in "Applied" stage to be forwarded.
    /// ApplicationId must be provided in the request body.
    /// </remarks>
    /// <param name="command">Forward command with ApplicationId and optional HR note</param>
    /// <returns>Updated application status</returns>
    [HttpPatch("forward")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(ForwardApplicationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Forward([FromBody] ForwardApplicationCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Application forwarded successfully.",
                data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    /// <summary>
    /// Schedule an interview for a shortlisted application
    /// </summary>
    /// <remarks>
    /// **Access:** DepartmentHead only (must be from the same department as the job posting)
    /// 
    /// Creates an interview record with specified interviewers and updates application stage to "InterviewScheduled".
    /// ApplicationId must be provided in the request body.
    /// 
    /// **Validations:**
    /// - Application must be in "Shortlisted" stage
    /// - ScheduledAt must be in the future
    /// - At least one interviewer is required
    /// - All interviewers must belong to the same enterprise
    /// </remarks>
    /// <param name="command">Interview scheduling details with ApplicationId</param>
    /// <returns>Created interview details with participants</returns>
    [HttpPost("schedule-interview")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    [ProducesResponseType(typeof(ScheduleInterviewResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Interview scheduled successfully.",
                data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

