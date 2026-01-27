using System;
using System.Threading.Tasks;

namespace ERMS.Application.Interface
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        Task<Guid?> GetEnterpriseIdAsync();
    }
}
