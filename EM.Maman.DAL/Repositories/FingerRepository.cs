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
    public class FingerRepository : Repository<Finger>, IFingerRepository
    {
        public FingerRepository(LocalMamanDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Finger>> GetFingersBySideAsync(int side)
        {
            return await Context.Fingers
                .Where(f => f.Side == side)
                .ToListAsync();
        }

        public async Task<IEnumerable<Finger>> GetFingersByPositionAsync(int position)
        {
            return await Context.Fingers
                .Where(f => f.Position == position)
                .ToListAsync();
        }
    }
}
