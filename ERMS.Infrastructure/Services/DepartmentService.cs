using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Infrastructure.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ERMSDbContext _context;

        public DepartmentService(ERMSDbContext context)
        {
            _context = context;
        }

        public async Task<Department> GetOrCreateHRDepartmentAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
        {
            // Try to find existing HR department
            var hrDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.EnterpriseId == enterpriseId && d.DepartmentCode == "HR" && !d.IsDeleted, cancellationToken);

            if (hrDepartment != null)
            {
                return hrDepartment;
            }

            // Create HR department if not exists
            hrDepartment = new Department
            {
                EnterpriseId = enterpriseId,
                DepartmentName = "Human Resources",
                DepartmentCode = "HR",
                Description = "Default HR Department",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(hrDepartment);
            await _context.SaveChangesAsync(cancellationToken);

            return hrDepartment;
        }

        public async Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
        }
    }
}
