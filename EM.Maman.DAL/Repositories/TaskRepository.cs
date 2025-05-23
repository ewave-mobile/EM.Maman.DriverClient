﻿using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DAL.Repositories
{
    public class TaskRepository : Repository<Models.LocalDbModels.Task>, ITaskRepository
    {
        public TaskRepository(LocalMamanDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Models.LocalDbModels.Task>> GetPendingTasksAsync()
        {
            return await Context.Tasks
                .Include(t => t.StorageSourceFinger)
                .Include(t => t.StorageDestinationCell)
                .Include(t => t.RetrievalSourceCell)
                .Include(t => t.RetrievalDestinationFinger)
                .Include(t => t.RetrievalDestinationCell)
                .Where(t => t.IsExecuted == false)
                .ToListAsync();
        }

        public async Task<IEnumerable<Models.LocalDbModels.Task>> GetTasksByPalletIdAsync(int palletId)
        {
            return await Context.Tasks
                .Include(t => t.StorageSourceFinger)
                .Include(t => t.StorageDestinationCell)
                .Include(t => t.RetrievalSourceCell)
                .Include(t => t.RetrievalDestinationFinger)
                .Include(t => t.RetrievalDestinationCell)
                .Where(t => t.PalletId == palletId)
                .ToListAsync();
        }

        public async Task<Models.LocalDbModels.Task> GetTaskWithDetailsAsync(int taskId)
        {
            return await Context.Tasks
                .Include(t => t.StorageSourceFinger)
                .Include(t => t.StorageDestinationCell)
                .Include(t => t.RetrievalSourceCell)
                .Include(t => t.RetrievalDestinationFinger)
                .Include(t => t.RetrievalDestinationCell)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }
    }
}
