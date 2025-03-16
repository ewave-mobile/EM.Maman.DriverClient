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
    public class CellRepository : Repository<Cell>, ICellRepository
    {
        public CellRepository(LocalMamanDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Cell>> GetCellsByPositionAsync(int position)
        {
            return await Context.Cells
                .Where(c => c.Position == position)
                .ToListAsync();
        }

        public async Task<IEnumerable<Cell>> GetCellsBySideAsync(int side)
        {
            return await Context.Cells
                .Where(c => c.Side == side)
                .ToListAsync();
        }

        public async Task<IEnumerable<Cell>> GetActiveOnlyCellsAsync()
        {
      // throw exception
      throw new NotImplementedException();
        }

        public async Task<Cell> GetCellWithPalletAsync(int cellId)
        {
            // Assuming you have relationships defined
            throw new NotImplementedException();
        }
    }
}
