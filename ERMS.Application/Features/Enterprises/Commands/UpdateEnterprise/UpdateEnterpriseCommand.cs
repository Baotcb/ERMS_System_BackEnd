using MediatR;
using System;

namespace ERMS.Application.Features.Enterprises.Commands.UpdateEnterprise
{
    public class UpdateEnterpriseCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string? EnterpriseName { get; set; }
        public string? EnterpriseCode { get; set; }
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public Guid? SubscriptionPlanId { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public string? SubscriptionStatus { get; set; }
    }
}
