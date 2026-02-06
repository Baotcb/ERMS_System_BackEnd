using MediatR;
using Microsoft.AspNetCore.Http;

namespace ERMS.Application.Features.Applications.Commands.SubmitApplication;

/// <summary>
/// Command to submit a job application with CV upload
/// </summary>
public sealed class SubmitApplicationCommand : IRequest<SubmitApplicationResult>
{
    /// <summary>
    /// The job posting to apply for
    /// </summary>
    public Guid JobPostingId { get; set; }

    /// <summary>
    /// CV/Resume file (PDF only, max 5MB)
    /// </summary>
    public IFormFile CvFile { get; set; } = null!;

    /// <summary>
    /// Optional cover letter
    /// </summary>
    public string? CoverLetter { get; set; }

    /// <summary>
    /// Expected salary (optional)
    /// </summary>
    public decimal? ExpectedSalary { get; set; }

    /// <summary>
    /// Earliest available start date (optional)
    /// </summary>
    public DateTime? AvailableStartDate { get; set; }
}
