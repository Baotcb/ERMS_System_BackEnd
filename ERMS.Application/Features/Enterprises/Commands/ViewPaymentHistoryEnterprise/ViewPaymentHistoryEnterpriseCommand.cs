using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Enterprises.Commands.ViewPaymentHistoryEnterprise
{
    public class ViewPaymentHistoryEnterpriseCommand : IRequest<ViewPaymentHistoryEnterpriseResponse>
    {
        public Guid EnterpriseId { get; set; }
    }

    public class ViewPaymentHistoryEnterpriseResponse
    {
        public List<SubscriptionHistoryDto> SubscriptionHistories { get; set; } = new List<SubscriptionHistoryDto>();
    }

    public class SubscriptionHistoryDto
    {
        public Guid Id { get; set; }
        public string ActionType { get; set; } = null!;
        public string PlanName { get; set; } = null!;
        public string? PreviousPlanName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? PaymentMethod { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public string? Note { get; set; }
    }
}
