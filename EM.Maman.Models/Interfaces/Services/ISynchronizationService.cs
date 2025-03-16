using EM.Maman.Models.CustomModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Services
{
    public interface ISynchronizationService
    {
        event EventHandler<SyncProgressEventArgs> SyncProgressChanged;

        bool IsSynchronizing { get; }
        int PendingOperationsCount { get; }

        Task<bool> SynchronizeAsync(CancellationToken cancellationToken = default);
        Task<int> GetPendingOperationsCountAsync();
        void StartAutomaticSync();
        void StopAutomaticSync();
    }
}
