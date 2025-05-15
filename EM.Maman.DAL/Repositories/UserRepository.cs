using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DAL.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(LocalMamanDBContext context) : base(context)
        {
        }

        public async Task<User> GetByCodeAsync(string code)
        {
            return await Context.Users
                .FirstOrDefaultAsync(u => u.EmployeeCode == code);
        }

        public async Task<bool> AuthenticateAsync(string code)
        {
            var user = await GetByCodeAsync(code);
            if (user != null)
            {
                user.LastLoginDate = DateTime.Now;
                Context.Update(user);
                return true;
            }
            return false;
        }
    }
}
