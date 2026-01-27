using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Infrastructure.Services
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly ERMSDbContext _context;

        public SubscriptionPlanService(ERMSDbContext context)
        {
            _context = context;
        }

        public async Task<SubscriptionPlan> GetOrCreateFreePlanAsync(CancellationToken cancellationToken = default)
        {
            var freePlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.PlanCode == "FREE" && !p.IsDeleted, cancellationToken);

            if (freePlan != null)
            {
                return freePlan;
            }

            // Create default FREE plan
            freePlan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
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

            return freePlan;
        }

        public async Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        }

        public async Task<SubscriptionPlan?> GetByCodeAsync(string planCode, CancellationToken cancellationToken = default)
        {
            return await _context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlanCode == planCode && !p.IsDeleted, cancellationToken);
        }
    }
}
