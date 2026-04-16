namespace ERMS.Application.Features.Applications.Commands.AddExternalApplication;

public sealed class AddExternalApplicationResult
{
    public Guid ApplicationId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
}
