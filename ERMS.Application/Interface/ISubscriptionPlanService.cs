using ERMS.Domain.Entities.Enterprise;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Interface
{
    public interface ISubscriptionPlanService
    {
        /// <summary>
        /// Get default free plan, create if not exists
        /// </summary>
        Task<SubscriptionPlan> GetOrCreateFreePlanAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get subscription plan by ID
        /// </summary>
        Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get subscription plan by code
        /// </summary>
        Task<SubscriptionPlan?> GetByCodeAsync(string planCode, CancellationToken cancellationToken = default);
    }
}
