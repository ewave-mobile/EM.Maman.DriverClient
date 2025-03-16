using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Repositories
{
    public interface IPalletRepository : IRepository<Pallet>
    {
        Task<IEnumerable<Pallet>> GetPalletsByTypeAsync(string type);
        Task<IEnumerable<Pallet>> GetAvailablePalletsAsync();
        Task<Pallet> GetPalletWithLogsAsync(int palletId);
    }
}
