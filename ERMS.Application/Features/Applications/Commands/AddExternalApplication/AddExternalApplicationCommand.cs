using MediatR;

namespace ERMS.Application.Features.Applications.Commands.AddExternalApplication;

public sealed class AddExternalApplicationCommand : IRequest<AddExternalApplicationResult>
{
    public Guid JobPostingId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CandidatePhone { get; set; }
    public string ResumeUrl { get; set; } = string.Empty;
    public string ResumeText { get; set; } = string.Empty;
}
