using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
// No need for System.Threading.Tasks using if fully qualified

namespace EM.Maman.DAL.Repositories
{
    public class ConfigurationRepository : Repository<Configuration>, IConfigurationRepository
    {

        public ConfigurationRepository(LocalMamanDBContext context) : base(context)
        {
            // Constructor logic if needed
        }
        
  
    }
}
