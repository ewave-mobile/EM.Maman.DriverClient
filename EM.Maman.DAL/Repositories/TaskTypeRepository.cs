using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore; // Required for DbContext
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DAL.Repositories
{
    /// <summary>
    /// Concrete implementation of the repository for TaskType entities.
    /// </summary>
    public class TaskTypeRepository : Repository<TaskType>, ITaskTypeRepository
    {
        /// <summary>
        /// Initializes a new instance of the TaskTypeRepository class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public TaskTypeRepository(LocalMamanDBContext context) : base(context)
        {
            // The base constructor handles setting the context and DbSet.
        }

        // Cast the context to the specific DbContext type if needed for specific queries
        // protected LocalMamanDBContext MamanContext => Context as LocalMamanDBContext;

        // Add any TaskType-specific data access methods here if needed in the future.
        // Example:
        // public async Task<TaskType> GetByCodeAsync(int code)
        // {
        //     return await DbSet.FirstOrDefaultAsync(tt => tt.Code == code);
        // }
    }
}
