using ERMS.Domain.Entities.Organization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Interface
{
    /// <summary>
    /// Service interface for Employee operations.
    /// Abstracts data access from Application layer.
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>
        /// Creates a new employee record
        /// </summary>
        Task<Guid> CreateAsync(Employee employee, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets employee by ID
        /// </summary>
        Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets employee by User ID
        /// </summary>
        Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all employees for an enterprise with pagination
        /// </summary>
        Task<List<Employee>> GetByEnterpriseAsync(Guid enterpriseId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets count of employees for an enterprise
        /// </summary>
        Task<int> CountByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates employee record
        /// </summary>
        Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes employee
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates unique employee code for enterprise
        /// </summary>
        Task<string> GenerateEmployeeCodeAsync(Guid enterpriseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple employees in bulk
        /// </summary>
        Task AddRangeAsync(IEnumerable<Employee> employees, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
