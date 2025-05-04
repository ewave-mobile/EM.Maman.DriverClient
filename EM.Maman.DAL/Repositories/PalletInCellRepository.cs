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

        // Implement PalletInCell specific methods here if needed in the future
    }
}
