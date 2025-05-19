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

        public override async Task<Pallet> GetByIdAsync(object id) // Changed signature to match base
        {
            if (id is int intId)
            {
                return await DbSet.FindAsync(intId);
            }
            // Handle cases where id is not an int, or throw an exception
            // For now, assuming Pallet.Id is always int and id will be passed as int.
            // Consider adding more robust type checking or conversion if needed.
            // This also implies Pallet.Id is indeed int.
            if (id is long longId && longId <= int.MaxValue && longId >= int.MinValue)
            {
                 // If it was passed as long but fits in int (e.g. from a less specific call)
                return await DbSet.FindAsync((int)longId);
            }
            // Throw an error or return null if the ID type is unexpected or cannot be converted
            // For safety, let's throw if it's not convertible to int, as Pallet.Id is int.
            throw new ArgumentException($"Pallet ID must be an integer. Received type: {id?.GetType().Name}", nameof(id));
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
