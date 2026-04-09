using ERMS.Application.Features.JobPostings.Commands.CreateJobPosting;
using ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;
using ERMS.Application.Features.JobPostings.Commands.PublishJobPosting;
using ERMS.Application.Features.JobPostings.Commands.CloseJobPosting;
using ERMS.Application.Features.JobPostings.Commands.DeleteJobPosting;
using ERMS.Application.Features.JobPostings.Commands.GenerateJD;
using ERMS.Application.Features.JobPostings.Queries.GetJobPostingById;
using ERMS.Application.Features.JobPostings.Queries.GetJobPostings;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERMS.Application.Features.JobPostings.Commands.SaveJobPosting;
using ERMS.Application.Features.JobPostings.Commands.UnsaveJobPosting;
using ERMS.Application.Features.JobPostings.Queries.GetMySavedPosts;

namespace ERMS.API.Controllers;

/// <summary>
/// Job Postings Management API
/// </summary>
[Route("api/job-postings")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class JobPostingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobPostingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all job postings with pagination and optional status filter
    /// </summary>
    /// <param name="query">Pagination and filter parameters</param>
    /// <returns>Paginated list of job postings</returns>
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.HRManager},{AppRoles.Director}")]
    [ProducesResponseType(typeof(GetJobPostingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] GetJobPostingsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get a job posting by ID with full details including PlanName and CampaignName
    /// </summary>
    /// <param name="id">Job posting ID</param>
    /// <returns>Job posting details</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = $"{AppRoles.HRManager},{AppRoles.Director}")]
    [ProducesResponseType(typeof(JobPostingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetJobPostingByIdQuery { Id = id });
            if (result == null)
                return NotFound(new { message = "Job posting not found." });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new job posting from an approved PlanDetail
    /// </summary>
    /// <remarks>
    /// Business Rules:
    /// - PlanDetail must have Status = "Approved"
    /// - PlanDetail.RequiredSkills must not be empty (required for AI CV scanning)
    /// - Quota check: HiredCount must be less than PlanDetail.Quantity
    /// </remarks>
    /// <param name="command">Create command with PlanDetailId and optional overrides</param>
    /// <returns>Created job posting ID</returns>
    [HttpPost]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateJobPostingCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var jobPostingId = await _mediator.Send(command);
            return Ok(new
            {
                message = "Job posting created successfully.",
                jobPostingId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a job posting (only mutable fields: Description, Benefits, Deadline, Location, RemoteOption)
    /// </summary>
    /// <remarks>
    /// Note: PlanDetailId is immutable and cannot be changed after creation.
    /// ID must be provided in the request body.
    /// </remarks>
    /// <param name="command">Update command with Id and fields to modify</param>
    /// <returns>Success message</returns>
    [HttpPut]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update([FromBody] UpdateJobPostingCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Job posting updated successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Publish a draft job posting to make it visible to candidates
    /// </summary>
    /// <remarks>
    /// Precondition: Job posting must be in "Draft" status.
    /// ID must be provided in the request body.
    /// </remarks>
    /// <param name="command">Command with job posting Id</param>
    /// <returns>Success message</returns>
    [HttpPatch("publish")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Publish([FromBody] PublishJobPostingCommand command)
    {
        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Job posting published successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Close a published job posting to stop receiving applications
    /// </summary>
    /// <remarks>
    /// Precondition: Job posting must be in "Published" status.
    /// ID must be provided in the request body.
    /// </remarks>
    /// <param name="command">Command with job posting Id</param>
    /// <returns>Success message</returns>
    [HttpPatch("close")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Close([FromBody] CloseJobPostingCommand command)
    {
        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Job posting closed successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft delete a job posting
    /// </summary>
    /// <remarks>ID must be provided in the request body.</remarks>
    /// <param name="command">Command with job posting Id</param>
    /// <returns>Success message</returns>
    [HttpDelete]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete([FromBody] DeleteJobPostingCommand command)
    {
        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Job posting deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("savejob")]
    [Authorize(Roles = AppRoles.Candidate)]
    public async Task<IActionResult> SaveJobPosting(SaveJobPostingCommand command)
    {
        try
        {
            var savedJobId = await _mediator.Send(command);
            return Ok(new
            {
                message = "Job posting saved successfully.",
                savedJobId
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
    /// Get the current candidate's saved job postings
    /// </summary>
    /// <remarks>
    /// **Access:** Candidate only
    /// 
    /// Returns a paginated list of all job postings saved by the authenticated candidate.
    /// Includes job details, company name, and save date.
    /// </remarks>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <returns>Paginated list of saved job postings</returns>
    [HttpGet("my-saved-posts")]
    [Authorize(Roles = AppRoles.Candidate)]
    [ProducesResponseType(typeof(GetMySavedPostsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMySavedPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new GetMySavedPostsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Unsave a job posting (Candidate only)
    /// </summary>
    /// <remarks>
    /// **Access:** Candidate only
    /// 
    /// Removes a saved job posting from the candidate's saved list.
    /// Returns 403 Forbidden if the candidate tries to unsave a post they don't own.
    /// **JobPostingId must be provided in the request body.**
    /// </remarks>
    /// <param name="command">Unsave command with JobPostingId</param>
    /// <returns>Confirmation of unsave</returns>
    [HttpDelete("unsave")]
    [Authorize(Roles = AppRoles.Candidate)]
    [ProducesResponseType(typeof(UnsaveJobPostingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnsaveJobPosting([FromBody] UnsaveJobPostingCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = result.Message,
                data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Generate a Job Description using AI based on a PlanDetail
    /// </summary>
    /// <remarks>
    /// **Access:** HRManager only
    ///
    /// Calls Gemini AI to generate a structured Vietnamese JD (description, requirements, benefits).
    /// Response is returned directly to the frontend — nothing is saved to the database.
    /// </remarks>
    /// <param name="command">Command with PlanDetailId</param>
    /// <returns>Generated JD with description, requirements, and benefits</returns>
    [HttpPost("generate-jd")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(GenerateJDResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GenerateJDResult>> GenerateJD([FromBody] GenerateJDCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
