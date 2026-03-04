namespace ERMS.Domain.Constants.Application;

/// <summary>
/// Decision constants for Interview entity - represents the final outcome of an interview round.
/// </summary>
public static class InterviewDecision
{
    public const string Fail = "Fail";
    public const string Passed = "Passed";
    public const string NextRound = "NextRound";

    public static readonly string[] ValidDecisions = [Fail, Passed, NextRound];

    public static bool IsValid(string decision)
        => Array.Exists(ValidDecisions, d => d.Equals(decision, StringComparison.OrdinalIgnoreCase));
}
