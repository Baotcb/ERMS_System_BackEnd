using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Commands.RegisterEnterprise
{
    public sealed class RegisterEnterpriseHandler : IRequestHandler<RegisterEnterpriseCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public RegisterEnterpriseHandler(
            IERMSDbContext context,
            ISubscriptionPlanService subscriptionPlanService)
        {
            _context = context;
            _subscriptionPlanService = subscriptionPlanService;
        }

        public async Task<Guid> Handle(RegisterEnterpriseCommand request, CancellationToken cancellationToken)
        {
            // 1. Check if Enterprise TaxCode already exists
            if (!string.IsNullOrEmpty(request.TaxCode))
            {
                var existingEnterprise = await _context.Enterprises
                    .AnyAsync(e => e.TaxCode == request.TaxCode && !e.IsDeleted, cancellationToken);
                if (existingEnterprise)
                {
                    throw new InvalidOperationException("Mã số thuế doanh nghiệp đã tồn tại.");
                }
            }

            // 2. Get Default Subscription Plan (via service - Clean Architecture)
            var freePlan = await _subscriptionPlanService.GetOrCreateFreePlanAsync(cancellationToken);

            // 3. Generate Enterprise Code
            var enterpriseCode = $"ENT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

            // 4. Create Enterprise
            var enterprise = new Enterprise
            {
                Id = Guid.NewGuid(),
                EnterpriseName = request.EnterpriseName,
                EnterpriseCode = enterpriseCode,
                TaxCode = request.TaxCode,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                Website = request.Website,
                LogoUrl = request.LogoUrl,
                
                SubscriptionPlanId = freePlan.Id,
                SubscriptionStartDate = DateTime.UtcNow,
                SubscriptionEndDate = DateTime.UtcNow.AddYears(100),
                SubscriptionStatus = "Active",
                
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Enterprises.Add(enterprise);
            await _context.SaveChangesAsync(cancellationToken);

            return enterprise.Id;
        }
    }
}
