using MediatR;

namespace ERMS.Application.Features.Applications.Commands.AddExternalApplication;

public sealed class AddExternalApplicationCommand : IRequest<AddExternalApplicationResult>
{
    public Guid JobPostingId { get; set; }
    public string CandidateName { get; set; } = null!;
    public string CandidateEmail { get; set; } = null!;
    public string? CandidatePhone { get; set; }
    public string ResumeUrl { get; set; } = null!;
    public string ResumeText { get; set; } = null!;
}
