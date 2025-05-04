using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
// No need for System.Threading.Tasks using if fully qualified

namespace EM.Maman.DAL.Repositories
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        protected readonly LocalMamanDBContext _context;
        protected readonly DbSet<Configuration> _dbSet;

        public ConfigurationRepository(LocalMamanDBContext context)
        {
            _context = context;
            _dbSet = context.Set<Configuration>();
        }

        /// <summary>
        /// Checks if any configuration record exists.
        /// </summary>
        /// <returns>True if at least one configuration exists, false otherwise.</returns>
        public async System.Threading.Tasks.Task<bool> AnyAsync() // Fully qualify Task
        {
            return await _dbSet.AnyAsync();
        }

        /// <summary>
        /// Adds a new configuration record.
        /// </summary>
        /// <param name="configuration">The configuration object to add.</param>
        /// <returns></returns>
        public System.Threading.Tasks.Task AddAsync(Configuration configuration) // Fully qualify Task, make synchronous body
        {
            // Use synchronous Add
            _dbSet.Add(configuration);
            // Return completed task to match interface signature
            return System.Threading.Tasks.Task.CompletedTask;
            // Note: SaveChanges is called by UnitOfWork.CompleteAsync()
        }
    }
}
