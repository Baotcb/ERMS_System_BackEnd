using ERMS.Application.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ERMS.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IERMSDbContext _context;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IERMSDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public Guid? UserId
        {
            get
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return userId != null ? Guid.Parse(userId) : null;
            }
        }

        public async Task<Guid?> GetEnterpriseIdAsync()
        {
            if (UserId == null) return null;

            // Check if context has access to Employees
            // We use AsNoTracking for performance as we only need the ID
            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == UserId && !e.IsDeleted);

            return employee?.EnterpriseId;
        }
    }
}
