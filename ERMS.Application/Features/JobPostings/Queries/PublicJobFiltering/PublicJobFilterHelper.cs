using System.Text.RegularExpressions;

namespace ERMS.Application.Features.JobPostings.Queries.PublicJobFiltering;

public static partial class PublicJobFilterHelper
{
    public static string NormalizeEmploymentType(string? employmentType)
    {
        if (string.IsNullOrWhiteSpace(employmentType))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(employmentType.Trim(), @"[\s\-_]+", string.Empty);
        return normalized.ToLowerInvariant() switch
        {
            "fulltime" => "FullTime",
            "parttime" => "PartTime",
            "contract" => "Contract",
            "internship" => "Internship",
            _ => employmentType.Trim()
        };
    }

    public static bool MatchesExperienceBucket(string? experienceLevel, string? bucket)
    {
        if (string.IsNullOrWhiteSpace(bucket))
        {
            return true;
        }

        if (!TryParseExperienceRange(experienceLevel, out var minYears, out var maxYears))
        {
            return bucket == "0";
        }

        if (bucket == "0")
        {
            return minYears == 0 && maxYears == 0;
        }

        if (bucket == "5+")
        {
            return maxYears >= 5;
        }

        var parts = bucket.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !decimal.TryParse(parts[0], out var filterMin)
            || !decimal.TryParse(parts[1], out var filterMax))
        {
            return false;
        }

        return minYears <= filterMax && maxYears >= filterMin;
    }

    public static int CalculateRelevanceScore(string jobTitle, string enterpriseName, string description, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return 0;
        }

        var normalized = searchTerm.Trim().ToLowerInvariant();

        if (jobTitle.Contains(normalized, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (enterpriseName.Contains(normalized, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return description.Contains(normalized, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    public static bool TryParseExperienceRange(string? experienceLevel, out decimal minYears, out decimal maxYears)
    {
        minYears = 0;
        maxYears = 0;

        if (string.IsNullOrWhiteSpace(experienceLevel))
        {
            return false;
        }

        var normalized = experienceLevel.Trim().ToLowerInvariant();
        if (normalized.Contains("không") || normalized.Contains("khong") || normalized.Contains("no experience"))
        {
            return true;
        }

        var numbers = ExperienceRegex()
            .Matches(normalized)
            .Select(match => decimal.TryParse(match.Value, out var value) ? value : (decimal?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        if (numbers.Length == 0)
        {
            return false;
        }

        minYears = numbers[0];
        maxYears = numbers.Length > 1 ? numbers[1] : numbers[0];
        return true;
    }

    [GeneratedRegex(@"\d+(\.\d+)?", RegexOptions.Compiled)]
    private static partial Regex ExperienceRegex();
}
