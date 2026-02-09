namespace ERMS.Domain.Constants.Application;

/// <summary>
/// Stage constants for Application entity - represents candidate progression through recruitment pipeline
/// </summary>
public static class ApplicationStage
{
    public const string Applied = "Applied";
    public const string Reviewing = "Reviewing";
    public const string Shortlisted = "Shortlisted";
    public const string InterviewScheduled = "InterviewScheduled";
    public const string Interviewed = "Interviewed";
    public const string Offered = "Offered";
    public const string Hired = "Hired";
    public const string Rejected = "Rejected";
    public const string Withdrawn = "Withdrawn";

    public static readonly string[] ValidStages = 
        [Applied, Reviewing, Shortlisted, InterviewScheduled, Interviewed, Offered, Hired, Rejected, Withdrawn];

    public static bool IsValid(string stage)
        => Array.Exists(ValidStages, s => s.Equals(stage, StringComparison.OrdinalIgnoreCase));

    public static bool IsHired(string stage)
        => stage.Equals(Hired, StringComparison.OrdinalIgnoreCase);

    public static bool IsShortlisted(string stage)
        => stage.Equals(Shortlisted, StringComparison.OrdinalIgnoreCase);

    public static bool CountsTowardsQuota(string stage)
        => IsHired(stage);
}
