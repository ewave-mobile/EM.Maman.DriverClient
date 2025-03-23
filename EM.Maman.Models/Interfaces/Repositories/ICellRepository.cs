using EM.Maman.Models.CustomModels;
using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Repositories
{
    public interface ICellRepository : IRepository<Cell>
    {
        Task<IEnumerable<Cell>> GetCellsByPositionAsync(int position);
        Task<IEnumerable<Cell>> GetCellsBySideAsync(int side);
        Task<IEnumerable<Cell>> GetActiveOnlyCellsAsync();
        Task<Cell> GetCellWithPalletAsync(int cellId);
        Task<IEnumerable<PalletInCell>> GetPalletsWithCellsAsync();

        Task<IEnumerable<CellWithPalletInfo>> GetCellsWithPalletsAsync();
    }
}
