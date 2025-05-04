using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Repositories
{
    /// <summary>
    /// Interface for the repository handling TaskType data operations.
    /// Inherits generic repository operations for the TaskType entity.
    /// </summary>
    public interface ITaskTypeRepository : IRepository<TaskType>
    {
        // Add any TaskType-specific methods here if needed in the future.
        // For now, the generic methods from IRepository are sufficient.
    }
}
