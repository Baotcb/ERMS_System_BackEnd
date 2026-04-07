using MediatR;

namespace ERMS.Application.Features.Reports.Commands.CreateReport;

public sealed class CreateReportCommand : IRequest<Guid>
{
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
}

