using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Repositories
{
    public interface ITaskRepository : IRepository<LocalDbModels.Task>
    {
        Task<IEnumerable<LocalDbModels.Task>> GetPendingTasksAsync();
        Task<IEnumerable<LocalDbModels.Task>> GetTasksByPalletIdAsync(string palletId);
        Task<LocalDbModels.Task> GetTaskWithDetailsAsync(int taskId);
    }
}
