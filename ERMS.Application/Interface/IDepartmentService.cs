using ERMS.Domain.Entities.Organization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Interface
{
    public interface IDepartmentService
    {
        /// <summary>
        /// Get or create default HR department for an enterprise
        /// </summary>
        Task<Department> GetOrCreateHRDepartmentAsync(Guid enterpriseId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get department by ID
        /// </summary>
        Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
