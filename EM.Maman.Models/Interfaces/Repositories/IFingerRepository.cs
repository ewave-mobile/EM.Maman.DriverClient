using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Repositories
{
    public interface IFingerRepository : IRepository<Finger>
    {
        Task<IEnumerable<Finger>> GetFingersBySideAsync(int side);
        Task<IEnumerable<Finger>> GetFingersByPositionAsync(int position);
    }
}
