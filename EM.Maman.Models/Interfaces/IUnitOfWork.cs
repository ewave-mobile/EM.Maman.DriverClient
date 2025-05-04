using EM.Maman.Models.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ITrolleyRepository Trolleys { get; }
        ICellRepository Cells { get; }
        IFingerRepository Fingers { get; }
        ILevelRepository Levels { get; } // Add Level repository
        ITaskRepository Tasks { get; }
        IOperationRepository Operations { get; }
        IPalletRepository Pallets { get; }
        IUserRepository Users { get; }
        IConfigurationRepository Configurations { get; }
        IPalletInCellRepository PalletInCells { get; } // Add PalletInCell repository
        ITaskTypeRepository TaskTypes { get; } // Added TaskType repository

        Task<int> CompleteAsync();
    }
}
