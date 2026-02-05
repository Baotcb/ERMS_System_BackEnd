using ERMS.Application.Features.PlanDetails.Commands.CreatePlanDetail;
using ERMS.Application.Features.PlanDetails.Commands.UpdatePlanDetail;
using ERMS.Application.Features.PlanDetails.Commands.DeletePlanDetail;
using ERMS.Application.Features.PlanDetails.Queries.GetAllPlanDetails;
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
    [Authorize(Roles =AppRoles.DepartmentHead + "," + AppRoles.Director)]
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
    /// </remarks>
    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanDetailCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "ID không khớp" });

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
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.DepartmentHead )]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeletePlanDetailCommand { Id = id });
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
}
