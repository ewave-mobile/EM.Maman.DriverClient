using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace EM.Maman.DAL.Repositories
{
    /// <summary>
    /// Repository implementation for trolley cell operations
    /// </summary>
    public class TrolleyCellRepository : Repository<TrolleyCell>, ITrolleyCellRepository
    {
        public TrolleyCellRepository(LocalMamanDBContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TrolleyCell>> GetByTrolleyIdAsync(long trolleyId)
        {
            return await Context.TrolleyCells
                .Where(tc => tc.TrolleyId == trolleyId)
                .Include(tc => tc.Pallet)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<TrolleyCell> GetByTrolleyAndPositionAsync(long trolleyId, string position)
        {
            return await Context.TrolleyCells
                .Where(tc => tc.TrolleyId == trolleyId && tc.Position == position)
                .Include(tc => tc.Pallet)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public async Task AddPalletToTrolleyCellAsync(long trolleyId, string position, int palletId)
        {
            // Get the cell or create it if it doesn't exist
            var cell = await GetByTrolleyAndPositionAsync(trolleyId, position);
            
            if (cell == null)
            {
                // Create a new cell
                cell = new TrolleyCell
                {
                    TrolleyId = trolleyId,
                    Position = position
                };
                await AddAsync(cell);
                await Context.SaveChangesAsync(); // Save to get the ID
            }
            
            // Update the cell with the pallet
            cell.PalletId = palletId;
            cell.StorageDate = DateTime.Now;
            
            Update(cell);
        }

        /// <inheritdoc/>
        public async Task RemovePalletFromTrolleyCellAsync(long trolleyId, string position)
        {
            var cell = await GetByTrolleyAndPositionAsync(trolleyId, position);
            
            if (cell != null && cell.PalletId.HasValue)
            {
                cell.PalletId = null;
                cell.StorageDate = null;
                Update(cell);
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Pallet>> GetPalletsOnTrolleyAsync(long trolleyId)
        {
            var cells = await Context.TrolleyCells
                .Where(tc => tc.TrolleyId == trolleyId && tc.PalletId.HasValue)
                .Include(tc => tc.Pallet)
                .ToListAsync();
            
            return cells.Select(c => c.Pallet).Where(p => p != null);
        }
    }
}
