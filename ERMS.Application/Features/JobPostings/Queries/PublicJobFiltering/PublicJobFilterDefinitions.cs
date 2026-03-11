namespace ERMS.Application.Features.JobPostings.Queries.PublicJobFiltering;

public sealed record PublicJobFilterOption(string Value, string Label);

public sealed record PublicSalaryBucket(string Value, string Label, decimal? MinSalary, decimal? MaxSalary);

public static class PublicJobFilterDefinitions
{
    public static readonly IReadOnlyList<PublicJobFilterOption> ExperienceBuckets =
    [
        new(string.Empty, "Tất cả kinh nghiệm"),
        new("0", "Chưa có kinh nghiệm"),
        new("0-1", "Dưới 1 năm"),
        new("1-2", "1 - 2 năm"),
        new("2-3", "2 - 3 năm"),
        new("3-5", "3 - 5 năm"),
        new("5+", "Trên 5 năm"),
    ];

    public static readonly IReadOnlyList<PublicSalaryBucket> SalaryBuckets =
    [
        new(string.Empty, "Tất cả mức lương", null, null),
        new("0-10", "Dưới 10 triệu", 0, 10_000_000),
        new("10-15", "10 - 15 triệu", 10_000_000, 15_000_000),
        new("15-20", "15 - 20 triệu", 15_000_000, 20_000_000),
        new("20-30", "20 - 30 triệu", 20_000_000, 30_000_000),
        new("30+", "Trên 30 triệu", 30_000_000, null),
    ];

    public static readonly IReadOnlyList<PublicJobFilterOption> EmploymentTypes =
    [
        new(string.Empty, "Tất cả hình thức"),
        new("FullTime", "Toàn thời gian"),
        new("PartTime", "Bán thời gian"),
        new("Contract", "Hợp đồng"),
        new("Internship", "Thực tập"),
    ];
}
