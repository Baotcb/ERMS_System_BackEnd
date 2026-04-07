using MediatR;
using System.Text.Json.Serialization;

namespace ERMS.Application.Features.Reports.Commands.ProcessReport;

public sealed class ProcessReportCommand : IRequest<Unit>
{
    [JsonIgnore]
    public Guid ReportId { get; set; }
    public string Action { get; set; } = null!;
    public string? AdminNote { get; set; }
}
