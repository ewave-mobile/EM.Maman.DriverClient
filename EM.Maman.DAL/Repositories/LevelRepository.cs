using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore; // Optional if base Repository handles context
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EM.Maman.DAL.Repositories
{
    public class LevelRepository : Repository<Level>, ILevelRepository
    {
        public LevelRepository(LocalMamanDBContext context) : base(context)
        {
            // Base constructor handles context injection
        }

        // Implement Level specific methods here if needed in the future
        // Example:
        // public async Task<Level> GetLevelByNumberAsync(int number)
        // {
        //     return await Context.Levels.FirstOrDefaultAsync(l => l.Number == number);
        // }
    }
}
