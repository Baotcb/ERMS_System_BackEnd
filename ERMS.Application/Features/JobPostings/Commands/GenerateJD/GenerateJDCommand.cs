using MediatR;

namespace ERMS.Application.Features.JobPostings.Commands.GenerateJD;

public sealed class GenerateJDCommand : IRequest<GenerateJDResult>
{
    public Guid PlanDetailId { get; set; }
    public string? UserPrompt { get; set; }
}
