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
        ITaskRepository Tasks { get; }
        IOperationRepository Operations { get; }
        IPalletRepository Pallets { get; }
        IUserRepository Users { get; }

        Task<int> CompleteAsync();
    }
}
