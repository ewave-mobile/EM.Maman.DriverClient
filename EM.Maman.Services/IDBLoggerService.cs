using EM.Maman.Models.LocalDbModels;
using System.Threading.Tasks;

namespace EM.Maman.Services
{
    public interface IDBLoggerService
    {
        System.Threading.Tasks.Task LogAsync(TraceLog logEntry);
    }
}
