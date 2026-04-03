using MediatR;

namespace ERMS.Application.Features.Admin.Queries.GetGlobalPaymentHistory;

public sealed class GetGlobalPaymentHistoryQuery : IRequest<GetGlobalPaymentHistoryResponse>
{
    public string? EnterpriseSearch { get; set; }
    public string? ActionType { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
