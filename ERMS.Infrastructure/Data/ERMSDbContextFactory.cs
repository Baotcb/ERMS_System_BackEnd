using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ERMS.Infrastructure.Data
{
    public class ERMSDbContextFactory : IDesignTimeDbContextFactory<ERMSDbContext>
    {
        public ERMSDbContext CreateDbContext(string[] args)
        {
           
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "../ERMS.API/appsettings.json"))
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ERMSDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);

            return new ERMSDbContext(optionsBuilder.Options);
        }
    }
}