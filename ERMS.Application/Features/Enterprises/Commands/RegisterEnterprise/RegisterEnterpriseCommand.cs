using MediatR;
using System;

namespace ERMS.Application.Features.Enterprises.Commands.RegisterEnterprise
{
    public class RegisterEnterpriseCommand : IRequest<Guid>
    {
        public string EnterpriseName { get; set; } = null!;
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
    }
}
