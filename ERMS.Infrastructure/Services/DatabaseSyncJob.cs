using ERMS.Application.Interface;
using ERMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Infrastructure.Services
{
    public class DatabaseSyncJob : IDatabaseSyncJob
    {
        private readonly ERMSDbContext _primaryContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseSyncJob> _logger;

        public DatabaseSyncJob(
            ERMSDbContext primaryContext, 
            IConfiguration configuration,
            ILogger<DatabaseSyncJob> logger)
        {
            _primaryContext = primaryContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ExecuteSyncAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Đang bắt đầu Đồng bộ Database (Full Backup)...");
            
            var backupOptions = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseSqlServer(_configuration.GetConnectionString("SecondaryConnection"))
                .Options;

            using var backupContext = new ERMSDbContext(backupOptions);
            
            // 1. Tạo database hoặc scheme nếu chưa tồn tại
            await backupContext.Database.EnsureCreatedAsync(cancellationToken);

            // 2. Bỏ qua ràng buộc khóa ngoại (Foreign Key) trên toán bộ BackupDB để tránh lỗi thứ tự
            await backupContext.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all'");

            // Lấy danh sách Entities trừ MigrationsHistory
            var entityTypes = _primaryContext.Model.GetEntityTypes()
                .Where(t => !t.IsOwned() && t.GetTableName() != "__EFMigrationsHistory")
                .ToList();

            var syncMethod = GetType().GetMethod(nameof(SyncTableAsync), BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var entityType in entityTypes)
            {
                var clrType = entityType.ClrType;
                var tableName = entityType.GetTableName();

                _logger.LogInformation($"[Sync] Deleting & Re-inserting table {tableName}...");

                try
                {
                    var genericMethod = syncMethod.MakeGenericMethod(clrType);
                    await (Task)genericMethod.Invoke(this, new object[] { backupContext, cancellationToken });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Sync] Error syncing table {tableName}.");
                }
            }

            // 3. Bật lại ràng buộc khóa ngoại
            await backupContext.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all'");

            _logger.LogInformation("Đồng bộ Database thành công.");
        }

        private async Task SyncTableAsync<T>(ERMSDbContext backupContext, CancellationToken cancellationToken) where T : class
        {
            // 1. Lấy tất cả dữ liệu từ database chính
            var data = await _primaryContext.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
            
            var entityType = backupContext.Model.FindEntityType(typeof(T));
            var tableName = entityType?.GetTableName();
            if (string.IsNullOrEmpty(tableName)) return;

            // 2. Xóa dữ liệu cũ của bảng trên Backup DB 
            await backupContext.Database.ExecuteSqlRawAsync($"DELETE FROM [{tableName}]", cancellationToken);

            if (!data.Any()) return;

            // Kiểm tra có cột Identity không (thường là khóa chính kiểu int)
            // Trong EF Core, cột Identity sẽ có DefaultValueGenerationStrategy hoặc thuộc tính OnAdd
            bool hasIdentity = entityType.GetProperties().Any(p => 
                   p.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd 
                && (p.ClrType == typeof(int) || p.ClrType == typeof(long)));

            // 3. Chuẩn bị Insert
            // SQL Server yêu cầu Identity Insert nằm chung session/transaction
            using var transaction = await backupContext.Database.BeginTransactionAsync(cancellationToken);

            try 
            {
                if (hasIdentity)
                {
                    await backupContext.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] ON", cancellationToken);
                }

                await backupContext.Set<T>().AddRangeAsync(data, cancellationToken);
                await backupContext.SaveChangesAsync(cancellationToken);

                if (hasIdentity)
                {
                    await backupContext.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] OFF", cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch(Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                // Giải phóng RAM (rất quan trọng khi đồng bộ database lớn)
                backupContext.ChangeTracker.Clear();
            }
        }
    }
}
