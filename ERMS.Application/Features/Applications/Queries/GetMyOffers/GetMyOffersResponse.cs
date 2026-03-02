namespace ERMS.Application.Features.Applications.Queries.GetMyOffers;

/// <summary>
/// Response containing the candidate's offers with pagination
/// </summary>
public sealed class GetMyOffersResponse
{
    public List<CandidateOfferDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public sealed class CandidateOfferDto
{
    public Guid OfferId { get; set; }
    public string? OfferCode { get; set; }
    public string Position { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public decimal Salary { get; set; }
    public string SalaryFrequency { get; set; } = null!;
    public string? Bonus { get; set; }
    public string? Benefits { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string? OfferLetterUrl { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? CandidateNote { get; set; }
}
