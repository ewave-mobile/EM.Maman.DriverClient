using EM.Maman.Models.LocalDbModels;
// No 'using System.Threading.Tasks;' here, will use fully qualified name

namespace EM.Maman.Models.Interfaces.Services
{
    public interface ICurrentUserContext
    {
        User CurrentUser { get; }
        System.Threading.Tasks.Task SetCurrentUserAsync(int userId); // Fully qualified
        void ClearCurrentUser();
        string GetToken();
        int GetCraneId(); // To provide the CraneId for API calls
    }
}
