namespace ERMS.Domain.Constants.Application;

/// <summary>
/// Stage constants for Application entity - represents candidate progression
/// </summary>
public static class ApplicationStage
{
    public const string Applied = "Applied";
    public const string Screened = "Screened";
    public const string Interviewed = "Interviewed";
    public const string Offered = "Offered";
    public const string Hired = "Hired";
    public const string Rejected = "Rejected";
    public const string Withdrawn = "Withdrawn";

    public static readonly string[] ValidStages = 
        [Applied, Screened, Interviewed, Offered, Hired, Rejected, Withdrawn];

    public static bool IsValid(string stage)
        => Array.Exists(ValidStages, s => s.Equals(stage, StringComparison.OrdinalIgnoreCase));

    public static bool IsHired(string stage)
        => stage.Equals(Hired, StringComparison.OrdinalIgnoreCase);

    public static bool CountsTowardsQuota(string stage)
        => IsHired(stage);
}
