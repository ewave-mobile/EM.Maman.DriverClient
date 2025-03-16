using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Repositories
{
    public interface ITrolleyRepository : IRepository<Trolley>
    {
        Task<Trolley> GetByPositionAsync(int position);
        Task<Trolley> GetWithMovementLogsAsync(int id);
        Task<IEnumerable<Trolley>> GetActiveTrolleysAsync();
    }
}
