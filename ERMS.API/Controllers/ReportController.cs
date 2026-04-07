using ERMS.Application.Features.Reports.Commands.CreateReport;
using ERMS.Application.Features.Reports.Commands.ProcessReport;
using ERMS.Application.Features.Reports.Queries.GetAllReports;
using ERMS.Application.Features.Reports.Queries.GetReportById;
using ERMS.Application.Features.Reports.Queries.GetReportStats;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("fixed")]
public class ReportController : ControllerBase
{
    private readonly ISender _sender;

    public ReportController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Candidate)]
    [ProducesResponseType(typeof(CreateReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateReportCommand command)
    {
        var reportId = await _sender.Send(command);
        return Ok(new CreateReportResponse("Report submitted successfully", reportId));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(GetAllReportsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllReportsQuery query)
    {
        var result = await _sender.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ReportDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _sender.Send(new GetReportByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpPut("{id:guid}/process")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ProcessReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Process(Guid id, [FromBody] ProcessReportCommand command)
    {
        command.ReportId = id;
        await _sender.Send(command);
        return Ok(new ProcessReportResponse("Report processed successfully"));
    }

    [HttpGet("stats")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ReportStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _sender.Send(new GetReportStatsQuery());
        return Ok(result);
    }
}

public sealed record CreateReportResponse(string Message, Guid ReportId);
public sealed record ProcessReportResponse(string Message);
