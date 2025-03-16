using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DAL.Repositories
{
    public class OperationRepository : Repository<PendingOperation>, IOperationRepository
    {
        public OperationRepository(LocalMamanDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PendingOperation>> GetPendingOperationsAsync()
        {
            return await Context.PendingOperations
                .Where(o => o.Status == Models.Enums.OperationStatus.Pending.ToString())
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetPendingOperationsCountAsync()
        {
            return await Context.PendingOperations
                .CountAsync(o => o.Status == Models.Enums.OperationStatus.Pending.ToString());
        }

        public async System.Threading.Tasks.Task MarkAsCompletedAsync(int id)
        {
            var operation = await Context.PendingOperations.FindAsync(id);
            if (operation != null)
            {
                operation.Status = Models.Enums.OperationStatus.Completed.ToString();
                operation.CompletedAt = DateTime.Now;
                Context.Update(operation);
            }
        }

        public async System.Threading.Tasks.Task MarkAsFailedAsync(int id, string errorMessage)
        {
            var operation = await Context.PendingOperations.FindAsync(id);
            if (operation != null)
            {
                operation.Status = Models.Enums.OperationStatus.Failed.ToString();
                operation.ErrorMessage = errorMessage;
                Context.Update(operation);
            }
        }
    }
}
