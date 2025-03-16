using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DAL.Repositories
{
    public class PalletRepository : Repository<Pallet>, IPalletRepository
    {
        public PalletRepository(LocalMamanDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Pallet>> GetPalletsByTypeAsync(string type)
        {
            return await Context.Pallets
                .Where(p => p.UldType == type)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pallet>> GetAvailablePalletsAsync()
        {
           throw new NotImplementedException();
        }

        public async Task<Pallet> GetPalletWithLogsAsync(int palletId)
        {
            throw new NotImplementedException();
        }
    }
}
