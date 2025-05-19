using EM.Maman.Models.LocalDbModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Repositories
{
    // Inherits common methods like AddRangeAsync from IRepository<PalletInCell>
    public interface IPalletInCellRepository : IRepository<PalletInCell>
    {
        Task<PalletInCell> GetByPalletAndCellAsync(int? palletId, long? cellId);
        // Define other PalletInCell specific methods here if needed in the future
    }
}
