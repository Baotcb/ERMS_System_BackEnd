using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Enterprises.Commands.GetUrlAvataEnterprise
{
    public class GetUrlAvataEnterpriseCommand : IRequest<GetUrlAvataEnterpriseResponse>
    {
    }

    public class GetUrlAvataEnterpriseResponse
    {
        public string? LogoUrl { get; set; }
        public string EnterpriseName { get; set; } = string.Empty;
    }
}
