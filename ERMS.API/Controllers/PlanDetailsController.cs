using ERMS.Application.Features.PlanDetails.Commands.CreatePlanDetail;
using ERMS.Application.Features.PlanDetails.Commands.UpdatePlanDetail;
using ERMS.Application.Features.PlanDetails.Commands.DeletePlanDetail;
using ERMS.Application.Features.PlanDetails.Queries.GetAllPlanDetails;
using ERMS.Application.Features.Applications.Queries.GetShortlistedApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERMS.Domain.Constants.Roles;

namespace ERMS.API.Controllers;

[Route("api/plan-details")]
[ApiController]
[Authorize]
public class PlanDetailsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlanDetailsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách chi tiết kế hoạch theo PlanId
    /// </summary>
    [HttpGet]
    [Authorize(Roles =AppRoles.DepartmentHead + "," + AppRoles.Director + "," + AppRoles.HRManager)]
    public async Task<IActionResult> GetAll([FromQuery] Guid recruitmentPlanId)
    {
        try
        {
            var query = new GetAllPlanDetailsQuery { RecruitmentPlanId = recruitmentPlanId };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo chi tiết kế hoạch mới (Department Head only)
    /// </summary>
    /// <remarks>
    /// Plan phải ở status Draft hoặc Rejected. Tự động recalculate TotalBudget.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    public async Task<IActionResult> Create([FromBody] CreatePlanDetailCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var planDetailId = await _mediator.Send(command);
            return Ok(new
            {
                message = "Tạo chi tiết kế hoạch tuyển dụng thành công",
                planDetailId
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
    /// Cập nhật chi tiết kế hoạch (Department Head only)
    /// </summary>
    /// <remarks>
    /// Plan phải ở status Draft hoặc Rejected. Tự động recalculate TotalBudget.
    /// ID must be provided in the request body.
    /// </remarks>
    [HttpPut]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    public async Task<IActionResult> Update([FromBody] UpdatePlanDetailCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Cập nhật chi tiết kế hoạch tuyển dụng thành công" });
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
    /// Xóa chi tiết kế hoạch (Department Head only)
    /// </summary>
    /// <remarks>
    /// Soft delete. Plan phải ở status Draft hoặc Rejected. Tự động recalculate TotalBudget.
    /// ID must be provided in the request body.
    /// </remarks>
    [HttpDelete]
    [Authorize(Roles = AppRoles.DepartmentHead )]
    public async Task<IActionResult> Delete([FromBody] DeletePlanDetailCommand command)
    {
        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Xóa chi tiết kế hoạch tuyển dụng thành công" });
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
    /// Get shortlisted applications for a plan detail (Department Head view)
    /// </summary>
    /// <remarks>
    /// **Access:** DepartmentHead only (must be from the same department as the recruitment plan)
    /// 
    /// Returns applications in "Shortlisted" stage, sorted by CVScreeningResult.OverallScore descending.
    /// Department Heads can only access plan details from their own department.
    /// </remarks>
    /// <param name="planDetailId">Plan detail ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <returns>Paginated list of shortlisted applications</returns>
    [HttpGet("{planDetailId}/shortlisted")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    [ProducesResponseType(typeof(GetShortlistedApplicationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetShortlisted(
        Guid planDetailId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new GetShortlistedApplicationsQuery
            {
                PlanDetailId = planDetailId,
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
}
