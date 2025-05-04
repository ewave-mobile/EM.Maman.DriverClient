using EM.Maman.Models.LocalDbModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Repositories
{
    // Inherits common methods like AddRangeAsync from IRepository<Level>
    public interface ILevelRepository : IRepository<Level>
    {
        // Define Level specific methods here if needed in the future
        // Example: Task<Level> GetLevelByNumberAsync(int number);
    }
}
