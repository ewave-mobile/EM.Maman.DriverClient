using EM.Maman.Models.Interfaces; // For IUnitOfWorkFactory
using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.LocalDbModels;
using Microsoft.Extensions.Logging; // For ILogger
using System.Linq; // Required for FirstOrDefault
// No 'using System.Threading.Tasks;' here, will use fully qualified name

namespace EM.Maman.DriverClient.Services
{
    public class CurrentUserContext : ICurrentUserContext
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly ILogger<CurrentUserContext> _logger;
        private User _currentUser;
        private Configuration _currentConfiguration; // To store craneId

        public CurrentUserContext(IUnitOfWorkFactory unitOfWorkFactory, ILogger<CurrentUserContext> logger)
        {
            _unitOfWorkFactory = unitOfWorkFactory ?? throw new System.ArgumentNullException(nameof(unitOfWorkFactory));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public User CurrentUser => _currentUser;

        public async System.Threading.Tasks.Task SetCurrentUserAsync(int userId) // Fully qualified Task
        {
            if (userId == 0)
            {
                _logger.LogWarning("SetCurrentUserAsync called with invalid userId (0). Clearing current user.");
                ClearCurrentUser();
                return; // Implicitly returns a completed Task
            }
            try
            {
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    _currentUser = await unitOfWork.Users.GetByIdAsync(userId);
                    if (_currentUser == null)
                    {
                        _logger.LogError("Failed to load user with ID {UserId} in SetCurrentUserAsync.", userId);
                    }
                    else
                    {
                        _logger.LogInformation("CurrentUserContext: User set to: {EmployeeCode}", _currentUser.EmployeeCode);
                        // Also load configuration to get CraneId
                        var configurations = await unitOfWork.Configurations.FindAsync(c => true); // Assuming one config row
                        _currentConfiguration = configurations.FirstOrDefault();
                        if (_currentConfiguration == null)
                        {
                            _logger.LogWarning("CurrentUserContext: No configuration found when setting user.");
                        }
                        else
                        {
                            _logger.LogInformation("CurrentUserContext: CraneId set to: {CraneId}", _currentConfiguration.CraneId);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading user with ID {UserId} in SetCurrentUserAsync.", userId);
                ClearCurrentUser(); // Clear user info on error
            }
        }

        public void ClearCurrentUser()
        {
            _currentUser = null;
            _currentConfiguration = null;
            _logger.LogInformation("CurrentUserContext: Current user and configuration cleared.");
        }

        public string GetToken() => _currentUser?.Token;

        // Returns CraneId from the loaded configuration, or 0 if not found/set.
        public int GetCraneId() => _currentConfiguration?.CraneId ?? 0;
    }
}
