using EM.Maman.Models.LocalDbModels;
// No need for System.Threading.Tasks here if we fully qualify

namespace EM.Maman.Models.Interfaces.Repositories
{
    public interface IConfigurationRepository :  IRepository<Configuration>
    {

    }
}
