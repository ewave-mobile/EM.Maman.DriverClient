using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Services
{
    public interface ICommandQueueService
    {
        Task<long> EnqueueCommandAsync<T>(string commandType, T parameters, int? userId = null);
        Task<bool> TryDequeueCommandAsync(PendingOperation operation);
        Task<T> DeserializeParametersAsync<T>(string serializedParameters);
        System.Threading.Tasks.Task MarkOperationCompletedAsync(long operationId);
        System.Threading.Tasks.Task MarkOperationFailedAsync(long operationId, string error);
        Task<int> GetPendingOperationsCountAsync();
    }
}
