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
    public class GetUrlAvataEnterpriseHandle : IRequestHandler<GetUrlAvataEnterpriseCommand, GetUrlAvataEnterpriseResponse>
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
                throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
            }

            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.Id == enterpriseId, cancellationToken);

            if (enterprise == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy doanh nghiệp với ID {enterpriseId}.");
            }

            return new GetUrlAvataEnterpriseResponse
            {
                LogoUrl = enterprise.LogoUrl,
                EnterpriseName = enterprise.EnterpriseName
            };
        }
    }
}
