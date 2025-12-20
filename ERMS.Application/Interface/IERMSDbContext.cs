using ERMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Interface
{
    public interface IERMSDbContext
    {
        DbSet<Department> Departments { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    }
}
