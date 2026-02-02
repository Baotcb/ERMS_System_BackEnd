using ERMS.Application.Features.RecruitmentCampaigns.Commands.CreateRecruitmentCampaign;
using ERMS.Application.Features.RecruitmentCampaigns.Commands.UpdateCampaignStatus;
using ERMS.Application.Features.RecruitmentCampaigns.Queries.GetAllRecruitmentCampaigns;
using ERMS.Application.Features.RecruitmentCampaigns.Queries.GetRecruitmentCampaignById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers;

[Route("api/recruitment-campaigns")]
[ApiController]
[Authorize]
public class RecruitmentCampaignsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecruitmentCampaignsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách chiến dịch tuyển dụng (phân trang)
    /// </summary>
    /// <remarks>
    /// Filter: ?status=Open để lấy các chiến dịch đang mở cho Dept Head chọn
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAllRecruitmentCampaignsQuery query)
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
    /// Lấy chi tiết chiến dịch tuyển dụng theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetRecruitmentCampaignByIdQuery { Id = id });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo chiến dịch tuyển dụng mới (HR Manager only)
    /// </summary>
    /// <remarks>
    /// Tạo chiến dịch với status Open tự động
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecruitmentCampaignCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var campaignId = await _mediator.Send(command);
            return Ok(new
            {
                message = "Tạo chiến dịch tuyển dụng thành công",
                campaignId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật trạng thái chiến dịch (HR Manager only)
    /// </summary>
    /// <remarks>
    /// Ví dụ: Đóng chiến dịch khi hết hạn hoặc đủ plan
    /// Workflow: Draft → Open → Closed → Archived
    /// </remarks>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var command = new UpdateCampaignStatusCommand
            {
                Id = id,
                NewStatus = request.NewStatus
            };

            await _mediator.Send(command);
            return Ok(new { message = $"Cập nhật trạng thái chiến dịch thành '{request.NewStatus}' thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class UpdateStatusRequest
{
    public string NewStatus { get; set; } = null!;
}
