namespace ERMS.Application.Features.JobPostings.Commands.GenerateJD;

public sealed class GenerateJDResult
{
    public string Description { get; set; } = null!;
    public string? Requirements { get; set; }
    public string? Benefits { get; set; }
    public Guid PlanDetailId { get; set; }
}
