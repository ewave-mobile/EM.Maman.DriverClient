using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Enums;
using EM.Maman.Models.Interfaces.Managers;
using EM.Maman.Models.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IUserManager _userManager;
        private readonly IConnectionManager _connectionManager;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger<LoginViewModel> _logger;

        private string _employeeCode;
        private string _status;
        private bool _isLoggingIn;
        private RelayCommand _loginCommand;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<LoginSuccessEventArgs> LoginSuccessful;

        public string EmployeeCode
        {
            get => _employeeCode;
            set => SetProperty(ref _employeeCode, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set
            {
                if (SetProperty(ref _isLoggingIn, value))
                {
                    // Update the login command's CanExecute
                   // LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand LoginCommand => _loginCommand ??= new RelayCommand(
            async param => await LoginAsync(),
            param => CanLogin());

        public string Version => $"גרסה {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        public LoginViewModel(
            IUserManager userManager,
            IConnectionManager connectionManager,
            IDispatcherService dispatcherService,
            ILogger<LoginViewModel> logger)
        {
            _userManager = userManager;
            _connectionManager = connectionManager;
            _dispatcherService = dispatcherService;
            _logger = logger;

            // Start connection monitoring
            _connectionManager.StartConnectionMonitoring();
            _connectionManager.ConnectionStateChanged += ConnectionManager_ConnectionStateChanged;

            // Initialize login form
            Status = "מוכן להתחברות";
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(EmployeeCode))
            {
                Status = "נא להזין מספר עובד";
                return;
            }

            IsLoggingIn = true;
            Status = "מתחבר...";

            try
            {
                bool isAuthenticated = await _userManager.AuthenticateAsync(EmployeeCode);

                if (isAuthenticated)
                {
                    _logger.LogInformation($"User {EmployeeCode} logged in successfully");

                    var currentUser = await _userManager.GetCurrentUserAsync();

                    // Raise the login successful event
                    OnLoginSuccessful(currentUser.Id);
                }
                else
                {
                    _logger.LogWarning($"Login failed for user code: {EmployeeCode}");
                    Status = "מספר עובד שגוי";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                Status = "שגיאה בהתחברות";
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private bool CanLogin()
        {
            return !IsLoggingIn && !string.IsNullOrWhiteSpace(EmployeeCode);
        }

        private void ConnectionManager_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            _dispatcherService.Invoke(() =>
            {
                // Update status based on connection state changes
                if (e.ConnectionType == ConnectionType.Network)
                {
                    Status = e.IsConnected ? "מוכן להתחברות" : "אין חיבור לרשת";
                }
            });
        }

        protected virtual void OnLoginSuccessful(int userId)
        {
            LoginSuccessful?.Invoke(this, new LoginSuccessEventArgs(userId));
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class LoginSuccessEventArgs : EventArgs
    {
        public int UserId { get; }

        public LoginSuccessEventArgs(int userId)
        {
            UserId = userId;
        }
    }
}
