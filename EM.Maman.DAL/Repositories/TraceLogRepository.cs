using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;

namespace EM.Maman.DAL.Repositories
{
    public class TraceLogRepository : Repository<TraceLog>, ITraceLogRepository
    {
        public TraceLogRepository(LocalMamanDBContext context) : base(context)
        {
        }

        // Implement any TraceLog-specific methods here if needed in the future
    }
}
