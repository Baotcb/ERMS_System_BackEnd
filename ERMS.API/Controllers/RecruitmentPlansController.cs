using ERMS.Application.Features.RecruitmentPlans.Commands.CreateRecruitmentPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.DeleteRecruitmentPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.UpdateRecruitmentPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.ApprovePlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.RejectPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.SubmitPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.ResubmitPlan;
using ERMS.Application.Features.RecruitmentPlans.Queries.GetAllRecruitmentPlans;
using ERMS.Application.Features.RecruitmentPlans.Queries.GetRecruitmentPlanById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERMS.Domain.Constants.Roles;

namespace ERMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RecruitmentPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecruitmentPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.HRManager +"," + AppRoles.Director + "," + AppRoles.DepartmentHead)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllRecruitmentPlansQuery query)
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
    [HttpGet("{id}")]
    [Authorize(Roles = AppRoles.HRManager + "," + AppRoles.Director + "," + AppRoles.DepartmentHead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetRecruitmentPlanByIdQuery { Id = id });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles =  AppRoles.DepartmentHead)]
    public async Task<IActionResult> Create([FromBody] CreateRecruitmentPlanCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var recruitmentPlanId = await _mediator.Send(command);
            return Ok(new
            {
                message = "Tạo kế hoạch tuyển dụng thành công",
                recruitmentPlanId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPut]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    public async Task<IActionResult> Update([FromBody] UpdateRecruitmentPlanCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Cập nhật kế hoạch tuyển dụng thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    public async Task<IActionResult> Delete([FromBody] DeleteRecruitmentPlanCommand command)
    {
        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Xóa kế hoạch tuyển dụng thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("approve")]
    [Authorize(Roles = AppRoles.Director)]
    public async Task<IActionResult> ApprovePlan([FromBody] ApprovePlanCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Phê duyệt kế hoạch tuyển dụng thành công" });
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

    [HttpPatch("reject")]
    [Authorize(Roles = AppRoles.Director)]
    public async Task<IActionResult> RejectPlan([FromBody] RejectPlanCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Từ chối kế hoạch tuyển dụng thành công" });
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
    /// Department Head submit kế hoạch tuyển dụng (Draft → Pending)
    /// </summary>
    /// <remarks>
    /// Chuyển status từ Draft → Pending để chờ Director duyệt
    /// </remarks>
    [HttpPatch("submit")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    public async Task<IActionResult> SubmitPlan([FromBody] SubmitPlanCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Submit kế hoạch tuyển dụng thành công. Đang chờ Director phê duyệt." });
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
    /// Department Head resubmit kế hoạch đã bị từ chối (Rejected → Pending)
    /// </summary>
    /// <remarks>
    /// Chuyển status từ Rejected → Pending sau khi chỉnh sửa
    /// </remarks>
    [HttpPatch("resubmit")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    public async Task<IActionResult> ResubmitPlan([FromBody] ResubmitPlanCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Resubmit kế hoạch tuyển dụng thành công. Đang chờ Director phê duyệt." });
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
