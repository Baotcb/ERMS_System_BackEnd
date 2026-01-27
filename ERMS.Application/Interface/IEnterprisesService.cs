using ERMS.Domain.Entities.Enterprise;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERMS.Application.Interface
{
    public interface IEnterprisesService
    {
        Task<Guid> CreateAsync(Enterprise enterprise);
        Task UpdateAsync(Enterprise enterprise);

        Task<Enterprise?> GetByIdAsync(Guid id);

        Task<List<Enterprise>> GetPagedAsync(int pageNumber, int pageSize);
        Task<int> CountAsync();
        Task<Enterprise?> GetByCodeAsync(string enterpriseCode);
    }
}
