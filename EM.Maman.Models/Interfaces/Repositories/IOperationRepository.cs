using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Repositories
{
    public interface IOperationRepository : IRepository<PendingOperation>
    {
        Task<IEnumerable<PendingOperation>> GetPendingOperationsAsync();
        Task<int> GetPendingOperationsCountAsync();
        System.Threading.Tasks.Task MarkAsCompletedAsync(int id);
        System.Threading.Tasks.Task MarkAsFailedAsync(int id, string errorMessage);
    }
}
