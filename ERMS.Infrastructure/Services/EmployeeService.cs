using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Infrastructure.Services
{
    /// <summary>
    /// Implementation of IEmployeeService.
    /// Handles all Employee data access operations.
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly ERMSDbContext _context;

        public EmployeeService(ERMSDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            await _context.Employees.AddAsync(employee, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }

        public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Employees
                .AsNoTracking()
                .Include(e => e.User)
                .Include(e => e.Department)
                .Include(e => e.Enterprise)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        }

        public async Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Employees
                .AsNoTracking()
                .Include(e => e.User)
                .Include(e => e.Department)
                .Include(e => e.Enterprise)
                .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);
        }

        public async Task<List<Employee>> GetByEnterpriseAsync(Guid enterpriseId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.Employees
                .AsNoTracking()
                .Include(e => e.User)
                .Include(e => e.Department)
                .Where(e => e.EnterpriseId == enterpriseId && !e.IsDeleted)
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
        {
            return await _context.Employees
                .CountAsync(e => e.EnterpriseId == enterpriseId && !e.IsDeleted, cancellationToken);
        }

        public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            employee.UpdatedAt = DateTime.UtcNow;
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
            if (employee != null)
            {
                employee.IsDeleted = true;
                employee.DeletedAt = DateTime.UtcNow;
                employee.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<string> GenerateEmployeeCodeAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
        {
            var enterprise = await _context.Enterprises
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == enterpriseId, cancellationToken);

            if (enterprise == null)
                throw new InvalidOperationException("Enterprise not found");

            var employeeCount = await _context.Employees
                .CountAsync(e => e.EnterpriseId == enterpriseId, cancellationToken);

            return $"{enterprise.EnterpriseCode}-{(employeeCount + 1):D4}";
        }

        public async Task AddRangeAsync(IEnumerable<Employee> employees, CancellationToken cancellationToken = default)
        {
            await _context.Employees.AddRangeAsync(employees, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
