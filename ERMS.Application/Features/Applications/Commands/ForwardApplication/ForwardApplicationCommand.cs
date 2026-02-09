using MediatR;

namespace ERMS.Application.Features.Applications.Commands.ForwardApplication;

/// <summary>
/// Command to forward (shortlist) an application from Applied to Shortlisted stage
/// </summary>
public sealed class ForwardApplicationCommand : IRequest<ForwardApplicationResult>
{
    /// <summary>
    /// The Application ID to forward
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Optional HR note to add to the application
    /// </summary>
    public string? HRNote { get; set; }
}
