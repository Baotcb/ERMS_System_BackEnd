namespace ERMS.Application.Features.Applications.Commands.AddExternalApplication;

public sealed class AddExternalApplicationResult
{
    public Guid ApplicationId { get; set; }
    public string Stage { get; set; } = null!;
    public DateTime AppliedAt { get; set; }
}
