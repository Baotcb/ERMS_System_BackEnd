using ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostingById;
using ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers;

/// <summary>
/// Public Job Listings API - Accessible by Guests and Candidates
/// </summary>
[Route("api/public/jobs")]
[ApiController]
[Produces("application/json")]
public class PublicJobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicJobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all published job postings (no authentication required)
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of published job postings from active enterprises.
    /// 
    /// **Features:**
    /// - Search by job title or description
    /// - Filter by location, employment type
    /// - Excludes expired postings (past application deadline)
    /// - Salary information only shown if employer opted to display it
    /// </remarks>
    /// <param name="query">Pagination and filter parameters</param>
    /// <returns>Paginated list of published job postings</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetPublicJobPostingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] GetPublicJobPostingsQuery query)
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
    /// Get a single published job posting by ID (no authentication required)
    /// </summary>
    /// <remarks>
    /// Returns detailed information about a specific job posting.
    /// Automatically increments the view count for analytics.
    /// </remarks>
    /// <param name="id">Job posting ID</param>
    /// <returns>Job posting details</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicJobPostingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetPublicJobPostingByIdQuery { Id = id });

            if (result == null)
                return NotFound(new { message = "Job posting not found or not available." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

