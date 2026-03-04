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

        public RegisterEnterpriseHandler(IERMSDbContext context)
        {
            _context = context;
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

            // 2. Get or Create Default Subscription Plan
            var freePlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.PlanCode == "FREE" && !p.IsDeleted, cancellationToken);

            if (freePlan == null)
            {
                // Create default FREE plan
                freePlan = new SubscriptionPlan
                {
                    Id = Guid.CreateVersion7(),
                    PlanName = "Free Plan",
                    PlanCode = "FREE",
                    Description = "Default free plan for new enterprises",
                    MaxUsers = 5,
                    MaxJobPostings = 2,
                    MaxCourses = 2,
                    PriceMonthly = 0,
                    PriceYearly = 0,
                    IsActive = true,
                    DisplayOrder = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubscriptionPlans.Add(freePlan);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // 3. Generate Enterprise Code
            var enterpriseCode = $"ENT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.CreateVersion7().ToString("N")[..4].ToUpper()}";

            // 4. Create Enterprise
            var enterprise = new Enterprise
            {
                Id = Guid.CreateVersion7(),
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
