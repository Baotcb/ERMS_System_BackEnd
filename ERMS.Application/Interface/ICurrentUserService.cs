using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERMS.Application.Interface
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        IEnumerable<string> Roles { get; }
        Task<Guid?> GetEnterpriseIdAsync();
        Task<int?> GetDepartmentIdAsync();
    }
}
