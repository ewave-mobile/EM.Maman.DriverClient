using EM.Maman.Models.Dtos;
using EM.Maman.Models.Enums;
using EM.Maman.Models.Interfaces; // For IUnitOfWork
using EM.Maman.Models.Interfaces.Services; // For IMamanHttpService, IConnectionManager, IDispatcherService
using EM.Maman.Models.LocalDbModels; // For User, Configuration
using EM.Maman.Models.CustomModels; // For ConnectionStateChangedEventArgs
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using EM.Maman.Services;

namespace EM.Maman.DriverClient.ViewModels
{
    public class CraneOption
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IMamanHttpService _mamanHttpService;
        private readonly IUnitOfWork _unitOfWork; // Or IUnitOfWorkFactory
        private readonly IConnectionManager _connectionManager;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger<LoginViewModel> _logger;

        private string _employeeCode;
        private string _status;
        private bool _isErrorStatus; // Added
        private bool _isLoggingIn;
        private RelayCommand _loginCommand;
        private int _selectedCraneId;
        private IEnumerable<CraneOption> _craneOptions;
        private bool _isCraneSelectionVisible;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<LoginSuccessEventArgs> LoginSuccessful;

        public string EmployeeCode
        {
            get => _employeeCode;
            set => SetProperty(ref _employeeCode, value, nameof(EmployeeCode), () => LoginCommand.RaiseCanExecuteChanged());
        }

        public int SelectedCraneId
        {
            get => _selectedCraneId;
            set => SetProperty(ref _selectedCraneId, value, nameof(SelectedCraneId), () => LoginCommand.RaiseCanExecuteChanged());
        }

        public IEnumerable<CraneOption> CraneOptions
        {
            get => _craneOptions;
            set => SetProperty(ref _craneOptions, value);
        }

        public bool IsCraneSelectionVisible
        {
            get => _isCraneSelectionVisible;
            set => SetProperty(ref _isCraneSelectionVisible, value);
        }

        public string Status
        {
            get => _status;
            set
            {
                // Determine if the new status is an error message
                bool isError = !(value == "מתחבר..." || value == "התחברות הצליחה!" || value == "התחברות אופליין הצליחה." || value == "מוכן להתחברות" || string.IsNullOrEmpty(value) || value == "אין חיבור לרשת");
                SetProperty(ref _status, value, nameof(Status));
                IsErrorStatus = isError; // Update IsErrorStatus based on the new status
            }
        }

        public bool IsErrorStatus
        {
            get => _isErrorStatus;
            private set => SetProperty(ref _isErrorStatus, value, nameof(IsErrorStatus));
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value, nameof(IsLoggingIn), () => LoginCommand.RaiseCanExecuteChanged());
        }

        public RelayCommand LoginCommand => _loginCommand ??= new RelayCommand(
            async param => await LoginAsync(),
            param => CanLogin());

        public string Version => $"גרסה {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        public LoginViewModel(
            IMamanHttpService mamanHttpService,
            IUnitOfWork unitOfWork, // Consider IUnitOfWorkFactory if UoW needs to be shorter-lived
            IConnectionManager connectionManager,
            IDispatcherService dispatcherService,
            ILogger<LoginViewModel> logger)
        {
            _mamanHttpService = mamanHttpService ?? throw new ArgumentNullException(nameof(mamanHttpService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            LoadCraneOptions();
            InitializeCraneSelectionVisibility();

            _connectionManager.StartConnectionMonitoring();
            _connectionManager.ConnectionStateChanged += ConnectionManager_ConnectionStateChanged;

            Status = "מוכן להתחברות"; // This will set IsErrorStatus to false
        }

        private void LoadCraneOptions()
        {
            CraneOptions = new List<CraneOption>
            {
                new CraneOption { Name = "Ludige", Id = 1 },
                new CraneOption { Name = "LPV", Id = 2 },
                new CraneOption { Name = "מצבר קטן", Id = 3 },
                new CraneOption { Name = "מצבר גדול", Id = 4 }
            };
            // Set a default selection if needed, e.g., the first one or based on stored config
            SelectedCraneId = CraneOptions.FirstOrDefault()?.Id ?? 0;
        }

        private async void InitializeCraneSelectionVisibility() // Made async for DB access
        {
            try
            {
                var configurations = await _unitOfWork.Configurations.FindAsync(c => true);
                var configuration = configurations.FirstOrDefault();
                IsCraneSelectionVisible = (configuration == null || configuration.CraneId == 0 || App.IsFirstInitialization);
                if (!IsCraneSelectionVisible && configuration != null)
                {
                    SelectedCraneId = configuration.CraneId; // Pre-select if already configured
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing crane selection visibility. Defaulting to visible.");
                IsCraneSelectionVisible = true; // Default to visible if there's an error
            }
        }
        
        public async System.Threading.Tasks.Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(EmployeeCode))
            {
                Status = "נא להזין מספר עובד"; // Error
                return;
            }

            if (IsCraneSelectionVisible && SelectedCraneId == 0)
            {
                Status = "נא לבחור עמדה/עגורן"; // Error
                return;
            }

            IsLoggingIn = true;
            Status = "מתחבר..."; // Not an error

            try
            {
                LoginResultDto loginResult = await _mamanHttpService.LoginAsync(EmployeeCode, SelectedCraneId);

                if (loginResult.IsSuccess)
                {
                    _logger.LogInformation($"User {EmployeeCode} logged in successfully via API.");
                    Status = "התחברות הצליחה!"; // Not an error
                    ServerLoginResponse serverData = loginResult.UserData;
                    
                    User localUser = await _unitOfWork.Users.GetByCodeAsync(serverData.EmployeeCode);
                    bool isNewUser = localUser == null;
                    if (isNewUser)
                    {
                        localUser = new User { EmployeeCode = serverData.EmployeeCode };
                    }

                    localUser.BackendId = serverData.UserID;
                    localUser.FirstName = serverData.FirstName;
                    localUser.LastName = serverData.LastName;
                    localUser.RoleID = serverData.RoleID ?? 0;
                    localUser.Token = serverData.Token;
                    localUser.LastLoginDate = DateTime.UtcNow;

                    if (isNewUser)
                    {
                        await _unitOfWork.Users.AddAsync(localUser);
                    }
                    else
                    {
                        // EF Core tracks changes, so just calling CompleteAsync is enough if localUser is tracked.
                        // If not tracked, you might need an Update method in your repository.
                        // For simplicity, assuming it's tracked or AddAsync handles AddOrUpdate.
                    }
                    await _unitOfWork.CompleteAsync(); // Save user

                    // Save/Update Configuration for CraneId
                    var configurations = await _unitOfWork.Configurations.FindAsync(c => true);
                    var configuration = configurations.FirstOrDefault();
                    if (configuration == null)
                    {
                        configuration = new Configuration();
                        await _unitOfWork.Configurations.AddAsync(configuration);
                    }
                    configuration.CraneId = SelectedCraneId;
                    configuration.InitializedByEmployeeId = EmployeeCode; // Or serverData.EmployeeCode
                    configuration.InitializedAt = DateTime.UtcNow; // Or serverData.LoginTime if available

                    // Store the WorkstationType string
                    //var selectedCraneOption = CraneOptions.FirstOrDefault(co => co.Id == SelectedCraneId);
                    //if (selectedCraneOption != null)
                    //{
                    //    configuration.WorkstationType = selectedCraneOption.Name;
                    //}
                    
                    await _unitOfWork.CompleteAsync(); // Save configuration

                    // Status already set to "התחברות הצליחה!"
                    OnLoginSuccessful(localUser.Id); // Pass local user ID
                }
                else if (loginResult.IsNetworkError)
                {
                    _logger.LogWarning($"Network error during login for {EmployeeCode}. Attempting offline login.");
                    Status = "אין חיבור לרשת או שלא ניתן להגיע לשרת. מנסה התחברות אופליין..."; // Error
                    
                    User localUser = await _unitOfWork.Users.GetByCodeAsync(EmployeeCode);
                    if (localUser != null)
                    {
                        localUser.LastLoginDate = DateTime.UtcNow;
                        await _unitOfWork.CompleteAsync();
                        _logger.LogInformation($"User {EmployeeCode} logged in successfully offline.");
                        Status = "התחברות אופליין הצליחה."; // Not an error
                        OnLoginSuccessful(localUser.Id);
                    }
                    else
                    {
                        _logger.LogWarning($"Offline login failed for {EmployeeCode}: User not found in local DB.");
                        Status = "התחברות אופליין נכשלה: משתמש לא קיים מקומית. נדרש חיבור רשת להתחברות ראשונית."; // Error
                    }
                }
                else // API login failed for other reasons (e.g., bad credentials, server-side validation)
                {
                    _logger.LogWarning($"API Login failed for {EmployeeCode}: {loginResult.ErrorMessage}");
                    Status = loginResult.ErrorMessage; // This will be an error
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Login error for user {EmployeeCode}");
                Status = "שגיאה קריטית בתהליך ההתחברות. אנא פנה לתמיכה."; // Error
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private bool CanLogin()
        {
            if (IsLoggingIn || string.IsNullOrWhiteSpace(EmployeeCode))
                return false;
            if (IsCraneSelectionVisible && SelectedCraneId == 0)
                return false;
            return true;
        }

        private void ConnectionManager_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            _dispatcherService.Invoke(() =>
            {
                if (e.ConnectionType == ConnectionType.Network)
                {
                    // Avoid overwriting specific login status messages like "מתחבר..." or other error messages
                    // Only update if current status is a non-error, neutral message.
                    if (!IsErrorStatus && (Status == "מוכן להתחברות" || Status == "אין חיבור לרשת" || string.IsNullOrEmpty(Status)))
                    {
                         Status = e.IsConnected ? "מוכן להתחברות" : "אין חיבור לרשת"; // "אין חיבור לרשת" will set IsErrorStatus
                    }
                }
            });
        }

        protected virtual void OnLoginSuccessful(int localUserId)
        {
            LoginSuccessful?.Invoke(this, new LoginSuccessEventArgs(localUserId));
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null, Action onChangedCallback = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            onChangedCallback?.Invoke();
            return true;
        }
    }

    public class LoginSuccessEventArgs : System.EventArgs // Explicitly System.EventArgs
    {
        public int LocalUserId { get; } // Changed to LocalUserId for clarity

        public LoginSuccessEventArgs(int localUserId)
        {
            LocalUserId = localUserId;
        }
    }
}
