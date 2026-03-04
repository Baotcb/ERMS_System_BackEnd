using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Interface;

/// <summary>
/// Work item representing a CV scoring task to be processed in the background.
/// </summary>
public record CvScoringWorkItem(
    Guid ApplicationId,
    string ResumeText,
    string JobDescription,
    string RequiredSkills,
    string? EducationLevel,
    string? ExperienceLevel
);

/// <summary>
/// Interface for a background task queue that processes CV scoring asynchronously.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Enqueues a CV scoring work item for background processing.
    /// </summary>
    ValueTask EnqueueAsync(CvScoringWorkItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues the next CV scoring work item. Blocks until an item is available.
    /// </summary>
    ValueTask<CvScoringWorkItem> DequeueAsync(CancellationToken cancellationToken);
}
