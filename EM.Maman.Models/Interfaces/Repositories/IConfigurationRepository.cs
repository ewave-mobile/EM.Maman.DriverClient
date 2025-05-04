using EM.Maman.Models.LocalDbModels;
// No need for System.Threading.Tasks here if we fully qualify

namespace EM.Maman.Models.Interfaces.Repositories
{
    public interface IConfigurationRepository
    {
        /// <summary>
        /// Checks if any configuration record exists.
        /// </summary>
        /// <returns>True if at least one configuration exists, false otherwise.</returns>
        System.Threading.Tasks.Task<bool> AnyAsync(); // Fully qualify Task

        /// <summary>
        /// Adds a new configuration record.
        /// </summary>
        /// <param name="configuration">The configuration object to add.</param>
        /// <returns></returns>
        System.Threading.Tasks.Task AddAsync(Configuration configuration); // Fully qualify Task

        // Add other methods like Get, Update, Delete if needed later
    }
}
