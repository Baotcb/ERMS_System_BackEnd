using ERMS.Application.Features.Applications.Commands.AcceptOffer;
using ERMS.Application.Features.Applications.Commands.AddExternalApplication;
using ERMS.Application.Features.Applications.Commands.ExtractCvInfo;
using ERMS.Application.Features.Applications.Commands.RespondOfferByToken;
using ERMS.Application.Features.Applications.Queries.GetAllApplications;
using ERMS.Application.Features.Applications.Commands.ConfirmHire;
using ERMS.Application.Features.Applications.Commands.AssignInterviewer;
using ERMS.Application.Features.Applications.Commands.ConfirmInterviewSchedule;
using ERMS.Application.Features.Applications.Commands.CancelOffer;
using ERMS.Application.Features.Applications.Commands.CreateOffer;
using ERMS.Application.Features.Applications.Commands.ForwardApplication;
using ERMS.Application.Features.Applications.Commands.RejectApplication;
using ERMS.Application.Features.Applications.Commands.RejectOffer;
using ERMS.Application.Features.Applications.Commands.SubmitApplication;
using ERMS.Application.Features.Applications.Commands.SubmitFinalDecision;
using ERMS.Application.Features.Applications.Commands.SubmitInterviewFeedback;
using ERMS.Application.Features.Applications.Commands.WithdrawApplication;
using ERMS.Application.Features.Applications.Queries.GetAllOfferByHR;
using ERMS.Application.Features.Applications.Queries.GetApplicationsByJob;
using ERMS.Application.Features.Applications.Queries.GetMyApplications;
using ERMS.Application.Features.Applications.Queries.GetMyOffers;
using ERMS.Application.Features.Applications.Queries.GetOfferByIdOfHR;
using ERMS.Application.Features.Interviews.Queries.GetAllInterviews;
using ERMS.Application.Features.Interviews.Queries.GetInterviewFeedbackById;
using ERMS.Application.Features.Interviews.Queries.GetInterviewsForFeedback;
using ERMS.Application.Features.Interviews.Queries.GetMyInterviews;
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
    /// Get the current user's assigned interviews as an interviewer
    /// </summary>
    /// <remarks>
    /// **Access:** Any authenticated employee who is an interview participant
    /// 
    /// Returns a paginated list of interviews where the current user is assigned as a participant.
    /// Includes candidate info, job title, schedule details, and the user's participation status.
    /// </remarks>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="statusFilter">Optional filter by interview status (e.g., "Scheduled", "Completed")</param>
    /// <returns>Paginated list of interviewer's interviews</returns>
    [HttpGet("my-interviews")]
    [ProducesResponseType(typeof(GetMyInterviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyInterviews(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? statusFilter = null)
    {
        try
        {
            var query = new GetMyInterviewsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                StatusFilter = statusFilter
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
    /// Get all interviews assigned to interviewers within the enterprise
    /// </summary>
    /// <remarks>
    /// **Access:** HR Manager, Director
    /// 
    /// Returns a paginated list of all interviews across the enterprise that have been assigned an interviewer.
    /// Includes detailed candidate info, job title, schedule details, and participant summaries.
    /// </remarks>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="statusFilter">Optional filter by interview status (e.g., "Scheduled", "Completed")</param>
    /// <returns>Paginated list of all interviews</returns>
    [HttpGet("all-interviews")]
    [Authorize(Roles = $"{AppRoles.HRManager},{AppRoles.Director}")]
    [ProducesResponseType(typeof(GetAllInterviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllInterviews(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? statusFilter = null)
    {
        try
        {
            var query = new GetAllInterviewsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                StatusFilter = statusFilter
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
    /// Reject an application from the HR side
    /// </summary>
    /// <remarks>
    /// **Access:** HR Manager only
    ///
    /// Allows HR to reject an application that is currently in Applied, Reviewing, or Shortlisted stage.
    /// A rejection reason is required and will be stored for downstream communication.
    /// </remarks>
    /// <param name="command">Reject command with ApplicationId and required rejection reason</param>
    /// <returns>Updated application status</returns>
    [HttpPatch("reject")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(RejectApplicationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectApplication([FromBody] RejectApplicationCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Application rejected successfully.",
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
    /// Withdraw a job application (Candidate only)
    /// </summary>
    /// <remarks>
    /// **Access:** Candidate only
    /// 
    /// Allows a candidate to withdraw their own active application.
    /// The application must not be in a terminal stage (Rejected, Hired, or already Withdrawn).
    /// </remarks>
    /// <param name="command">Withdraw command with ApplicationId and optional reason</param>
    /// <returns>Updated application status</returns>
    [HttpPatch("withdraw")]
    [Authorize(Roles = AppRoles.Candidate)]
    [ProducesResponseType(typeof(WithdrawApplicationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawApplicationCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Application withdrawn successfully.",
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
    /// Assign interviewers to a shortlisted application
    /// </summary>
    /// <remarks>
    /// **Access:** DepartmentHead only
    /// 
    /// Department Head assigns interviewers to a shortlisted application.
    /// Creates an interview record with status 'PendingSchedule'.
    /// Does NOT update application stage yet.
    /// </remarks>
    /// <param name="command">Assignment details with ApplicationId and InterviewerIds</param>
    /// <returns>Created interview details</returns>
    [HttpPost("assign-interviewer")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    [ProducesResponseType(typeof(AssignInterviewerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignInterviewer([FromBody] AssignInterviewerCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Interviewers assigned successfully.",
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
    /// Confirm and schedule the interview
    /// </summary>
    /// <remarks>
    /// **Access:** HR Manager only
    /// 
    /// HR Manager sets the date, time, and location for the interview.
    /// Updates interview status to 'Scheduled' and application stage to 'InterviewScheduled'.
    /// Generates a Google Meet link.
    /// </remarks>
    /// <param name="command">Scheduling details with ApplicationId</param>
    /// <returns>Confirmed interview details with meeting link</returns>
    [HttpPost("confirm-schedule")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(ConfirmInterviewScheduleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmSchedule([FromBody] ConfirmInterviewScheduleCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Interview scheduled and confirmed successfully.",
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
    /// Submit individual interviewer feedback for an interview (Stage 1)
    /// </summary>
    /// <remarks>
    /// **Access:** Authenticated employee who is a participant of the interview
    /// 
    /// Allows an interviewer to submit their individual rating, feedback, and recommendation.
    /// Updates only the InterviewParticipant record.
    /// Does NOT change Interview status or Application stage.
    /// </remarks>
    /// <param name="command">Feedback details (ApplicationId, InterviewId, Rating, Feedback, Recommendation)</param>
    /// <returns>Submitted feedback confirmation</returns>
    [HttpPost("submit-interview-feedback")]
    [ProducesResponseType(typeof(SubmitInterviewFeedbackResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitFeedback([FromBody] SubmitInterviewFeedbackCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Interview feedback submitted successfully.",
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
    /// Submit the final decision on an interview (Stage 2)
    /// </summary>
    /// <remarks>
    /// **Access:** DepartmentHead of the same department as the job posting
    /// 
    /// Sets the interview Decision and updates Interview status to 'Completed'.
    /// Triggers the workflow:
    /// - **Fail**: Application stage → Rejected
    /// - **Passed**: Application stage → OfferProcessing
    /// - **NextRound**: Creates a new Interview with Round + 1
    /// </remarks>
    /// <param name="command">Decision details (ApplicationId, InterviewId, Decision, OverallRating, OverallFeedback, Note)</param>
    /// <returns>Decision result with updated application stage</returns>
    [HttpPost("submit-final-decision")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    [ProducesResponseType(typeof(SubmitFinalDecisionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitFinalDecision([FromBody] SubmitFinalDecisionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Final interview decision submitted successfully.",
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
    /// Get all completed interviews with submitted feedback for the caller's department
    /// </summary>
    /// <remarks>
    /// **Access:** Department Head only
    /// 
    /// Returns a paginated summary list of all interviews in the Department Head's department
    /// that are `Completed` and have had feedback submitted by at least one participant.
    /// </remarks>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <returns>Paginated summary of interviews awaiting final review</returns>
    [HttpGet("department/interviews-feedback")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    [ProducesResponseType(typeof(GetInterviewsForFeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInterviewsForFeedback(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new GetInterviewsForFeedbackQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
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
    /// Get detailed interview feedback for a specific completed interview
    /// </summary>
    /// <remarks>
    /// **Access:** Department Head only
    /// 
    /// Retrieves the exact, detailed individual feedback submitted by all interviewers 
    /// to assist the Department Head in making the final decision.
    /// </remarks>
    /// <param name="id">The Interview ID</param>
    /// <returns>Detailed candidate and individual feedback data</returns>
    [HttpGet("department/interviews-feedback/{id}")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    [ProducesResponseType(typeof(GetInterviewFeedbackByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInterviewFeedbackById(Guid id)
    {
        try
        {
            var query = new GetInterviewFeedbackByIdQuery { InterviewId = id };
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
    /// Get the current candidate's application history
    /// </summary>
    /// <remarks>
    /// **Access:** Candidate only
    /// 
    /// Returns a paginated list of all applications submitted by the authenticated candidate.
    /// Includes job details, current stage/status, AI screening score, and flags for interview/offer existence.
    /// </remarks>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="stageFilter">Optional filter by stage (e.g., "Applied", "Shortlisted", "Offered")</param>
    /// <returns>Paginated list of candidate's applications</returns>
    [HttpGet("my-applications")]
    [Authorize(Roles = AppRoles.Candidate)]
    [ProducesResponseType(typeof(GetMyApplicationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyApplications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? stageFilter = null)
    {
        try
        {
            var query = new GetMyApplicationsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                StageFilter = stageFilter
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
    /// Get the current candidate's job offers
    /// </summary>
    /// <remarks>
    /// **Access:** Candidate only
    /// 
    /// Returns a paginated list of all offers associated with the candidate's applications.
    /// Includes offer details, job title, department, salary, and current status.
    /// </remarks>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <returns>Paginated list of candidate's offers</returns>
    [HttpGet("my-offers")]
    [Authorize(Roles = AppRoles.Candidate)]
    [ProducesResponseType(typeof(GetMyOffersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyOffers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new GetMyOffersQuery
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
    /// Accept a job offer (Candidate only)
    /// </summary>
    /// <remarks>
    /// **Access:** Candidate only
    /// 
    /// Allows a candidate to accept an offer that has been sent to them.
    /// The offer must have status "Sent" and must not be expired.
    /// Upon acceptance, the offer status changes to "Accepted".
    /// The application stage remains unchanged — the final "Hired" state is reserved for HR's Confirm Hire action.
    /// **OfferId must be provided in the request body.**
    /// </remarks>
    /// <param name="command">Accept command with OfferId</param>
    /// <returns>Updated offer and application status</returns>
    [HttpPatch("accept-offer")]
    [Authorize(Roles = AppRoles.Candidate)]
    [ProducesResponseType(typeof(AcceptOfferResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AcceptOffer([FromBody] AcceptOfferCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Offer accepted successfully.",
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
    /// Reject a job offer (Candidate only)
    /// </summary>
    /// <remarks>
    /// **Access:** Candidate only
    /// 
    /// Allows a candidate to reject an offer that has been sent to them.
    /// The offer must have status "Sent".
    /// Upon rejection, the offer status changes to "Rejected" and the application stage also moves to "Rejected".
    /// A CandidateNote is required to explain the rejection.
    /// **OfferId must be provided in the request body.**
    /// </remarks>
    /// <param name="command">Reject command with OfferId and required CandidateNote</param>
    /// <returns>Updated offer status</returns>
    [HttpPatch("reject-offer")]
    [Authorize(Roles = AppRoles.Candidate)]
    [ProducesResponseType(typeof(RejectOfferResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectOffer([FromBody] RejectOfferCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Offer rejected successfully.",
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
    [HttpPost("offers")]
    [Authorize(Roles = AppRoles.HRManager)]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferCommand command)
    {
        try
        {
            var offerId = await _mediator.Send(command);
            return Ok(new
            {
                message = "Offer created and sent to candidate successfully.",
                offerId = offerId
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
    /// Cancel a job offer (HR Manager only)
    /// </summary>
    /// <remarks>
    /// **Access:** HR Manager only
    ///
    /// Allows an HR Manager to cancel an offer that has been sent or accepted.
    /// The offer must have status "Sent" or "Accepted".
    /// Upon cancellation, the offer status changes to "Cancelled" and the application stage moves to "Rejected".
    /// A CancellationReason is required — it is sent to the candidate via email but NOT saved to the database.
    /// **OfferId and CancellationReason must be provided in the request body.**
    /// </remarks>
    /// <param name="command">Cancel command with OfferId and required CancellationReason</param>
    /// <returns>Updated offer and application status</returns>
    [HttpPatch("cancel-offer")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(CancelOfferResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelOffer([FromBody] CancelOfferCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Offer đã được hủy thành công.",
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


    [HttpGet("hr/my-offers")]
    [Authorize(Roles = $"{AppRoles.HRManager},{AppRoles.Director}")]
    public async Task<IActionResult> GetMyCreatedOffers()
    {
        try
        {
            var query = new GetAllOfferByHRQuery();
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


    [HttpGet("hr/my-offers/{id}")]
    [Authorize(Roles = $"{AppRoles.HRManager},{AppRoles.Director}")]
    public async Task<IActionResult> GetCreatedOfferById(Guid id)
    {
        try
        {
            var query = new GetOfferByIdOfHRQuery { Id = id };
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
    /// Confirm hiring a candidate after contract signing (HR Manager only)
    /// </summary>
    /// <remarks>
    /// **Access:** HR Manager only
    /// 
    /// Triggered after the candidate physically signs the contract.
    /// Creates a new corporate User account (Employee role) and Employee profile.
    /// Updates the Application stage to "Hired".
    /// Sends the generated account credentials to the new employee via email.
    /// **ApplicationId and EmployeeEmail must be provided in the request body.**
    /// </remarks>
    /// <param name="command">Confirm hire command with ApplicationId and EmployeeEmail</param>
    /// <returns>Created employee details</returns>
    [HttpPost("confirm-hire")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(ConfirmHireResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmHire([FromBody] ConfirmHireCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Hire confirmed successfully. Employee account created.",
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
    /// Get all applications across the enterprise (HR Manager only)
    /// </summary>
    /// <remarks>
    /// **Access:** HR Manager only
    /// 
    /// Returns a paginated list of all applications submitted to any job posting
    /// belonging to the authenticated HR's enterprise.
    /// Includes basic candidate info, job title, stage, applied date, CV URL, and AI overall score.
    /// </remarks>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="stageFilter">Optional filter by stage (e.g., "Applied", "Shortlisted")</param>
    /// <returns>Paginated list of enterprise applications</returns>
    [HttpGet("enterprise")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(GetAllApplicationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllApplications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? stageFilter = null)
    {
        try
        {
            var query = new GetAllApplicationsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                StageFilter = stageFilter
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
    /// Upload a CV and extract candidate contact info (name, email, phone) using AI.
    /// Step 1 of the HR-add-external-candidate flow.
    /// </summary>
    [HttpPost("extract-cv-info")]
    [Authorize(Roles = AppRoles.HRManager)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ExtractCvInfoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExtractCvInfo([FromForm] ExtractCvInfoCommand command)
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

    /// <summary>
    /// HR adds an external candidate (no system account) directly to a job posting.
    /// Step 2 of the HR-add-external-candidate flow — call after extract-cv-info.
    /// </summary>
    [HttpPost("add-external")]
    [Authorize(Roles = AppRoles.HRManager)]
    [ProducesResponseType(typeof(AddExternalApplicationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddExternalApplication([FromBody] AddExternalApplicationCommand command)
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

    /// <summary>
    /// Public endpoint for external candidates to accept or reject an offer via email token link.
    /// No authentication required — token acts as the credential.
    /// </summary>
    [HttpPost("offer-response/{token:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespondOfferByTokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RespondOfferByToken(Guid token, [FromQuery] string action)
    {
        try
        {
            var command = new RespondOfferByTokenCommand { Token = token, Action = action ?? string.Empty };
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
