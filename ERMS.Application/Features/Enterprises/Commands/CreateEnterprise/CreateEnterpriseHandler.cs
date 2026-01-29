using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Commands.CreateEnterprise
{
    public class CreateEnterpriseHandler
        : IRequestHandler<CreateEnterpriseCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateEnterpriseHandler(
            IERMSDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            CreateEnterpriseCommand request,
            CancellationToken cancellationToken)
        {
            var existingEnterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.EnterpriseCode == request.EnterpriseCode && !e.IsDeleted, cancellationToken);

            if (existingEnterprise != null)
            {
                throw new Exception("Mã doanh nghiệp đã tồn tại");
            }

            var enterprise = new Enterprise
            {
                Id = Guid.NewGuid(),
                EnterpriseName = request.EnterpriseName,
                EnterpriseCode = request.EnterpriseCode,
                TaxCode = request.TaxCode,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                Website = request.Website,
                LogoUrl = request.LogoUrl,

                SubscriptionPlanId = request.SubscriptionPlanId,
               SubscriptionStartDate = DateTime.UtcNow,
                SubscriptionEndDate = DateTime.UtcNow.AddYears(1),
                CreatedById = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Enterprises.Add(enterprise);
            await _context.SaveChangesAsync(cancellationToken);
            return enterprise.Id;
        }
    }
}
