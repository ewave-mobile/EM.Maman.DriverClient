using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Managers
{
    public interface IUserManager
    {
        Task<User> GetUserByCodeAsync(string code);
        Task<bool> AuthenticateAsync(string code);
        Task<User> GetCurrentUserAsync();
        void SetCurrentUser(User user);
        void ClearCurrentUser();
    }
}
