using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Commands.GetUrlAvataEnterprise
{
    internal class GetUrlAvataEnterpriseHandle : IRequestHandler<GetUrlAvataEnterpriseCommand, GetUrlAvataEnterpriseResponse>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetUrlAvataEnterpriseHandle(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetUrlAvataEnterpriseResponse> Handle(GetUrlAvataEnterpriseCommand request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
            {
                throw new UnauthorizedAccessException("User is not associated with any enterprise.");
            }

            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.Id == enterpriseId, cancellationToken);

            if (enterprise == null)
            {
                throw new KeyNotFoundException($"Enterprise with ID {enterpriseId} not found.");
            }

            return new GetUrlAvataEnterpriseResponse
            {
                LogoUrl = enterprise.LogoUrl,
                EnterpriseName = enterprise.EnterpriseName
            };
        }
    }
}
