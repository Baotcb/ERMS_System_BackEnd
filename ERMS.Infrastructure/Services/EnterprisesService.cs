using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERMS.Infrastructure.Services
{
    public class EnterprisesService : IEnterprisesService
    {
        private readonly ERMSDbContext _context;

        public EnterprisesService(ERMSDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateAsync(Enterprise enterprise)
        {
            if (enterprise == null)
                throw new ArgumentNullException(nameof(enterprise));

            await _context.Enterprises.AddAsync(enterprise);
            await _context.SaveChangesAsync();

            return enterprise.Id;
        }

        public async Task UpdateAsync(Enterprise enterprise)
        {
            if (enterprise == null)
                throw new ArgumentNullException(nameof(enterprise));

            _context.Enterprises.Update(enterprise);
            await _context.SaveChangesAsync();
        }

        public async Task<Enterprise?> GetByIdAsync(Guid id)
        {
            return await _context.Enterprises
                .AsNoTracking()
                .Include(x => x.SubscriptionPlan)
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<List<Enterprise>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _context.Enterprises
                .AsNoTracking()
                .Include(x => x.SubscriptionPlan)
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.Enterprises
                .AsNoTracking()
                .CountAsync(x => !x.IsDeleted);
        }

        public async Task<Enterprise?> GetByCodeAsync(string enterpriseCode)
        {
            return await _context.Enterprises
                .FirstOrDefaultAsync(x => x.EnterpriseCode == enterpriseCode && !x.IsDeleted);
        }
    }
}
