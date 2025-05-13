using EM.Maman.Models.Interfaces.Repositories;
using System;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Repository properties
        ITrolleyRepository Trolleys { get; }
        ITrolleyCellRepository TrolleyCells { get; }
        ICellRepository Cells { get; }
        IFingerRepository Fingers { get; }
        ILevelRepository Levels { get; }
        ITaskRepository Tasks { get; }
        IOperationRepository Operations { get; }
        IPalletRepository Pallets { get; }
        IPalletInCellRepository PalletInCells { get; }
        IUserRepository Users { get; }
        IConfigurationRepository Configurations { get; }
        ITaskTypeRepository TaskTypes { get; }

        // Save changes
        Task<int> CompleteAsync();

        // Transaction management
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
