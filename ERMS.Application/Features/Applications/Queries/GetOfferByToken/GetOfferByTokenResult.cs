namespace ERMS.Application.Features.Applications.Queries.GetOfferByToken;

public sealed class GetOfferByTokenResult
{
    public string CandidateName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public decimal Salary { get; set; }
    public string SalaryFrequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime ExpirationDate { get; set; }
}
