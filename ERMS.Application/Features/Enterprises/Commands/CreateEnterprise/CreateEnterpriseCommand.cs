using MediatR;
using System;

namespace ERMS.Application.Features.Enterprises.Commands.CreateEnterprise
{
    public class CreateEnterpriseCommand : IRequest<Guid>
    {
        public string EnterpriseName { get; set; } = null!;
        public string EnterpriseCode { get; set; } = null!;

        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }

        public Guid SubscriptionPlanId { get; set; }
    }
}
