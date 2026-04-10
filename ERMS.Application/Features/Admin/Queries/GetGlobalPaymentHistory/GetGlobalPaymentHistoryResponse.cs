namespace ERMS.Application.Features.Admin.Queries.GetGlobalPaymentHistory;

public sealed class GetGlobalPaymentHistoryResponse
{
    public List<GlobalPaymentHistoryItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public sealed class GlobalPaymentHistoryItemDto
{
    public Guid Id { get; set; }
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string EnterpriseCode { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? PlanName { get; set; }
    public string? PlanCode { get; set; }
    public string? PreviousPlanName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
