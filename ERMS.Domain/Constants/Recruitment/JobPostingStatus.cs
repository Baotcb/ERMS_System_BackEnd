namespace ERMS.Domain.Constants.Recruitment;

/// <summary>
/// Status constants for JobPosting entity
/// </summary>
public static class JobPostingStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Closed = "Closed";
    public const string Archived = "Archived";

    public static readonly string[] ValidStatuses = [Draft, Published, Closed, Archived];

    public static bool IsValid(string status)
        => Array.Exists(ValidStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));

    public static bool IsDraft(string status)
        => status.Equals(Draft, StringComparison.OrdinalIgnoreCase);

    public static bool IsPublished(string status)
        => status.Equals(Published, StringComparison.OrdinalIgnoreCase);

    public static bool CanPublish(string currentStatus)
        => IsDraft(currentStatus);

    public static bool CanClose(string currentStatus)
        => IsPublished(currentStatus);
}
