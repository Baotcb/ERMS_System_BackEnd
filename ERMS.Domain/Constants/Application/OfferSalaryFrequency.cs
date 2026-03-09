namespace ERMS.Domain.Constants.Application;

public static class OfferSalaryFrequency
{
    public const string Monthly = "Monthly";
    public const string Yearly = "Yearly";

    public static readonly string[] ValidFrequencies = [Monthly, Yearly];

    public static bool IsValid(string? frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
        {
            return false;
        }

        var normalized = frequency.Trim();
        return Array.Exists(
            ValidFrequencies,
            value => value.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string? frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
        {
            throw new ArgumentException("Salary frequency is required.", nameof(frequency));
        }

        var normalized = frequency.Trim();

        if (normalized.Equals(Monthly, StringComparison.OrdinalIgnoreCase))
        {
            return Monthly;
        }

        if (normalized.Equals(Yearly, StringComparison.OrdinalIgnoreCase))
        {
            return Yearly;
        }

        throw new ArgumentException($"Invalid salary frequency '{frequency}'.", nameof(frequency));
    }
}
