using ERMS.Application.Features.JobPostings.Commands.CreateJobPosting;
using ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;
using ERMS.Application.Features.JobPostings.Commands.PublishJobPosting;
using ERMS.Application.Features.JobPostings.Commands.CloseJobPosting;
using ERMS.Application.Features.JobPostings.Commands.DeleteJobPosting;
using ERMS.Application.Features.JobPostings.Queries.GetJobPostingById;
using ERMS.Application.Features.JobPostings.Queries.GetJobPostings;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERMS.Application.Features.JobPostings.Commands.SaveJobPosting;

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
    /// </remarks>
    /// <param name="id">Job posting ID</param>
    /// <param name="command">Update command with fields to modify</param>
    /// <returns>Success message</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobPostingCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "ID mismatch." });

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
    /// </remarks>
    /// <param name="id">Job posting ID</param>
    /// <returns>Success message</returns>
    [HttpPatch("{id}/publish")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Publish(Guid id)
    {
        try
        {
            await _mediator.Send(new PublishJobPostingCommand { Id = id });
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
    /// </remarks>
    /// <param name="id">Job posting ID</param>
    /// <returns>Success message</returns>
    [HttpPatch("{id}/close")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Close(Guid id)
    {
        try
        {
            await _mediator.Send(new CloseJobPostingCommand { Id = id });
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
    /// <param name="id">Job posting ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteJobPostingCommand { Id = id });
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
}
