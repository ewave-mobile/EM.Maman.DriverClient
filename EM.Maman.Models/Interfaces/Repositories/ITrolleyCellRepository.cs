using EM.Maman.Models.LocalDbModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace EM.Maman.Models.Interfaces.Repositories
{
    /// <summary>
    /// Repository interface for trolley cell operations
    /// </summary>
    public interface ITrolleyCellRepository : IRepository<TrolleyCell>
    {
        /// <summary>
        /// Gets all cells for a specific trolley
        /// </summary>
        /// <param name="trolleyId">The ID of the trolley</param>
        /// <returns>A collection of trolley cells</returns>
        Task<IEnumerable<TrolleyCell>> GetByTrolleyIdAsync(long trolleyId);

        /// <summary>
        /// Gets a specific cell on a trolley by position
        /// </summary>
        /// <param name="trolleyId">The ID of the trolley</param>
        /// <param name="position">The position on the trolley (e.g., "Left" or "Right")</param>
        /// <returns>The trolley cell, or null if not found</returns>
        Task<TrolleyCell> GetByTrolleyAndPositionAsync(long trolleyId, string position);

        /// <summary>
        /// Adds or updates a pallet in a trolley cell
        /// </summary>
        /// <param name="trolleyId">The ID of the trolley</param>
        /// <param name="position">The position on the trolley (e.g., "Left" or "Right")</param>
        /// <param name="palletId">The ID of the pallet to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task AddPalletToTrolleyCellAsync(long trolleyId, string position, int palletId);

        /// <summary>
        /// Removes a pallet from a trolley cell
        /// </summary>
        /// <param name="trolleyId">The ID of the trolley</param>
        /// <param name="position">The position on the trolley (e.g., "Left" or "Right")</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RemovePalletFromTrolleyCellAsync(long trolleyId, string position);

        /// <summary>
        /// Gets all pallets on a trolley
        /// </summary>
        /// <param name="trolleyId">The ID of the trolley</param>
        /// <returns>A collection of pallets on the trolley</returns>
        Task<IEnumerable<Pallet>> GetPalletsOnTrolleyAsync(long trolleyId);
    }
}
