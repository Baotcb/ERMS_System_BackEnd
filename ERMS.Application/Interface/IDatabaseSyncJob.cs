using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Interface
{
    public interface IDatabaseSyncJob
    {
        Task ExecuteSyncAsync(CancellationToken cancellationToken);
    }
}
