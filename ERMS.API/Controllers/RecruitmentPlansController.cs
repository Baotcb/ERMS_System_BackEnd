using ERMS.Application.Features.RecruitmentPlans.Commands.CreateRecruitmentPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.DeleteRecruitmentPlan;
using ERMS.Application.Features.RecruitmentPlans.Commands.UpdateRecruitmentPlan;
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

    /// <summary>
    /// Lấy danh sách kế hoạch tuyển dụng (phân trang)
    /// </summary>
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

    /// <summary>
    /// Lấy chi tiết kế hoạch tuyển dụng theo ID
    /// </summary>
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

    /// <summary>
    /// Tạo kế hoạch tuyển dụng mới
    /// </summary>
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

    /// <summary>
    /// Cập nhật kế hoạch tuyển dụng
    /// </summary>
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

    /// <summary>
    /// Xóa kế hoạch tuyển dụng (soft delete)
    /// </summary>
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
}
