using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore; // Optional if base Repository handles context
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EM.Maman.DAL.Repositories
{
    public class PalletInCellRepository : Repository<PalletInCell>, IPalletInCellRepository
    {
        public PalletInCellRepository(LocalMamanDBContext context) : base(context)
        {
            // Base constructor handles context injection
        }

        public async Task<PalletInCell> GetByPalletAndCellAsync(int? palletId, long? cellId)
        {
            if (!palletId.HasValue || !cellId.HasValue)
            {
                return null;
            }

            return await Context.PalletInCells
                .FirstOrDefaultAsync(pic => pic.PalletId == palletId.Value && pic.CellId == cellId.Value);
        }

        // Implement other PalletInCell specific methods here if needed in the future
    }
}
