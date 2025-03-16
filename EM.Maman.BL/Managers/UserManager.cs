using EM.Maman.Models.Interfaces;
using EM.Maman.Models.Interfaces.Managers;
using EM.Maman.Models.LocalDbModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.BL.Managers
{
    public class UserManager : IUserManager
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserManager> _logger;
        private User _currentUser;

        public UserManager(
            IUnitOfWork unitOfWork,
            ILogger<UserManager> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<User> GetUserByCodeAsync(string code)
        {
            try
            {
                return await _unitOfWork.Users.GetByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get user by code: {code}");
                return null;
            }
        }

        public async Task<bool> AuthenticateAsync(string code)
        {
            try
            {
                var result = await _unitOfWork.Users.AuthenticateAsync(code);

                if (result)
                {
                    // Get the authenticated user and set as current user
                    var user = await GetUserByCodeAsync(code);

                    if (user != null)
                    {
                        SetCurrentUser(user);

                        // Update last login date
                        user.LastLoginDate = DateTime.Now;
                        await _unitOfWork.CompleteAsync();

                        _logger.LogInformation($"User {user.Name} (ID: {user.Id}) authenticated successfully");
                    }
                }
                else
                {
                    _logger.LogWarning($"Authentication failed for user code: {code}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Authentication error for user code: {code}");
                return false;
            }
        }

        public Task<User> GetCurrentUserAsync()
        {
            return System.Threading.Tasks.Task.FromResult(_currentUser);
        }

        public void SetCurrentUser(User user)
        {
            _currentUser = user;
            _logger.LogInformation($"Current user set to: {user.Name} (ID: {user.Id})");
        }

        public void ClearCurrentUser()
        {
            _currentUser = null;
            _logger.LogInformation("Current user cleared");
        }
    }
}
