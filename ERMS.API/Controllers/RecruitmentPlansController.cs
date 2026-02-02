using ERMS.Application.Features.RecruitmentPlans.Commands.CreateRecruitmentPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.DeleteRecruitmentPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.UpdateRecruitmentPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.ApprovePlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.RejectPlan;
using ERMS.Application.Features.RecruitmentPlans.Queries.GetAllRecruitmentPlans;
using ERMS.Application.Features.RecruitmentPlans.Queries.GetRecruitmentPlanById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRecruitmentPlanCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "ID không khớp" });

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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteRecruitmentPlanCommand { Id = id });
            return Ok(new { message = "Xóa kế hoạch tuyển dụng thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("approve")]
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
}
