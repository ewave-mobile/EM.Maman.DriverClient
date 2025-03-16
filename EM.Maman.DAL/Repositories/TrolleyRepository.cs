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
    public class TrolleyRepository : Repository<Trolley>, ITrolleyRepository
    {
        public TrolleyRepository(LocalMamanDBContext context) : base(context)
        {
        }

        public async Task<Trolley> GetByPositionAsync(int position)
        {
            return await Context.Trolleys
                .FirstOrDefaultAsync(t => t.Position == position);
        }

        public async Task<Trolley> GetWithMovementLogsAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Trolley>> GetActiveTrolleysAsync()
        {
            throw new NotImplementedException();
        }
    }
}
