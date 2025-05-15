using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.PlcModels;
using EM.Maman.Models.LocalDbModels;
using System.Collections.ObjectModel;
using EM.Maman.Models.DisplayModels;
using EM.Maman.DAL;
using EM.Maman.Services;
using Microsoft.Extensions.Logging;
using EM.Maman.Models.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// Main view model that coordinates between different parts of the application
    /// </summary>
    public partial class MainViewModel
    {
        #region Fields

        private readonly ILogger<MainViewModel> _logger;
        private readonly IOpcService _opcService;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        public readonly IDispatcherService _dispatcherService;

        private bool _isWarehouseViewActive = true;
        private bool _isMapViewActive = false;
        private bool _isFingerAuthenticationViewActive = false;
        private int? _currentFingerPositionValue = null;
        private string _currentFingerDisplayName;
        private ObservableCollection<Finger> _availableFingers = new ObservableCollection<Finger>();
        private Trolley _currentTrolley;

        #endregion

        #region Properties

        public bool IsSimulationMode { get; private set; }
        public string ConnectionStatus { get; private set; } = "Disconnected";

        public bool IsWarehouseViewActive
        {
            get => _isWarehouseViewActive;
            set
            {
                if (_isWarehouseViewActive != value)
                {
                    _isWarehouseViewActive = value;
                    OnPropertyChanged(nameof(IsWarehouseViewActive));
                }
            }
        }

        public bool IsFingerAuthenticationViewActive
        {
            get => _isFingerAuthenticationViewActive;
            set
            {
                if (_isFingerAuthenticationViewActive != value)
                {
                    _isFingerAuthenticationViewActive = value;
                    OnPropertyChanged(nameof(IsFingerAuthenticationViewActive));
                    OnPropertyChanged(nameof(IsDefaultTaskViewActive));
                }
            }
        }

        public bool IsDefaultTaskViewActive => !ShouldShowTasksPanel;

        /// <summary>
        /// Determines whether to show the tasks panel (gray background) in the current task view.
        /// Shows the panel when: 1) At a finger location, 2) Has active storage tasks, 3) Has active retrieval tasks, 4) At a cell with retrieval task
        /// </summary>
        public bool ShouldShowTasksPanel
        {
            get
            {
                // Show tasks panel when:
                // 1. At a finger location
                // 2. Has active storage tasks
                // 3. Has active retrieval tasks
                // 4. At a cell with retrieval task
                return IsFingerAuthenticationViewActive || 
                       HasPalletsReadyForStorage || 
                       HasPalletsForRetrieval || 
                       IsAtCellWithRetrievalTask;
            }
        }

        /// <summary>
        /// Determines whether to show the default photo in the current task view.
        /// Shows the photo when: 1) No active tasks, 2) Not at a finger location, 3) Not at a cell with retrieval task
        /// </summary>
        public bool ShouldShowDefaultPhoto
        {
            get
            {
                // Show default photo when not showing tasks panel
                return !ShouldShowTasksPanel;
            }
        }

        /// <summary>
        /// Determines if the current position corresponds to a cell with a retrieval task.
        /// This will be implemented in the future for retrieval tasks.
        /// </summary>
        private bool IsAtCellWithRetrievalTask
        {
            get
            {
                // This will be implemented in the future for retrieval tasks
                // For now, return false since retrieval tasks aren't implemented yet
                return false;
            }
        }

        public bool IsMapViewActive
        {
            get => _isMapViewActive;
            set
            {
                if (_isMapViewActive != value)
                {
                    _isMapViewActive = value;
                    OnPropertyChanged(nameof(IsMapViewActive));
                }
            }
        }

        public Trolley CurrentTrolley
        {
            get => _currentTrolley;
            set
            {
                if (_currentTrolley != value)
                {
                    _currentTrolley = value;
                    OnPropertyChanged(nameof(CurrentTrolley));
                }
            }
        }

        public string CurrentFingerDisplayName
        {
            get => _currentFingerDisplayName;
            set
            {
                if (_currentFingerDisplayName != value)
                {
                    _currentFingerDisplayName = value;
                    OnPropertyChanged(nameof(CurrentFingerDisplayName));
                }
            }
        }

        public ObservableCollection<Finger> AvailableFingers
        {
            get => _availableFingers;
            private set
            {
                if (_availableFingers != value)
                {
                    _availableFingers = value;
                    OnPropertyChanged(nameof(AvailableFingers));
                }
            }
        }

        #endregion

        #region View Models

        public OpcViewModel OpcVM { get; }
        public TrolleyViewModel TrolleyVM { get; }
        public TrolleyOperationsViewModel TrolleyOperationsVM { get; }
        public WarehouseViewModel WarehouseVM { get; }
        public TaskViewModel TaskVM { get; }

        #endregion

        #region Collections

        public ObservableCollection<PalletAuthenticationItem> PalletsToAuthenticate { get; } = new ObservableCollection<PalletAuthenticationItem>();
        public ObservableCollection<PalletStorageTaskItem> PalletsReadyForStorage { get; } = new ObservableCollection<PalletStorageTaskItem>();
        public ObservableCollection<PalletRetrievalTaskItem> PalletsForRetrieval { get; } = new ObservableCollection<PalletRetrievalTaskItem>();
        public bool HasPalletsReadyForStorage => PalletsReadyForStorage.Any();
        public bool HasPalletsForRetrieval => PalletsForRetrieval.Any();

        #endregion

        #region Constructor

        public MainViewModel(
            IOpcService opcService,
            IUnitOfWorkFactory unitOfWorkFactory,
            IConnectionManager connectionManager,
            IDispatcherService dispatcherService,
            ILoggerFactory loggerFactory,
            TrolleyViewModel trolleyViewModel,
            IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<MainViewModel>();
            _opcService = opcService;
            _unitOfWorkFactory = unitOfWorkFactory;
            _dispatcherService = dispatcherService;
            IsSimulationMode = configuration.GetValue<bool>("AppSettings:UseSimulationMode");

            _logger.LogInformation("MainViewModel constructor START");

            // CurrentTrolley will be loaded from DB during InitializeApplicationAsync.
            // For now, TrolleyOperationsVM needs a non-null Trolley. We'll use a temporary one,
            // which will be replaced by the DB-loaded instance in InitializeApplicationAsync.
            var tempTrolleyForOpsVm = new Trolley { Id = 1, DisplayName = "Temp Initializing Trolley", Position = 1 };


            // Initialize view models
            TrolleyVM = trolleyViewModel;
            
            // Create a UnitOfWork instance for WarehouseViewModel initialization
            using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
            {
                WarehouseVM = new WarehouseViewModel(unitOfWork);
            }

            // Subscribe to connection changes
            opcService.ConnectivityChanged += (s, isConnected) =>
            {
                ConnectionStatus = isConnected ? "Connected" : "Disconnected";
                OnPropertyChanged(nameof(ConnectionStatus));
            };

            // Create the OPC view model
            OpcVM = new OpcViewModel(
                opcService,
                dispatcherService,
                loggerFactory.CreateLogger<OpcViewModel>());

            // Subscribe to position changes from OPC
            OpcVM.PositionChanged += OnPositionChanged;

            // Initialize TrolleyOperationsViewModel with a temporary trolley instance.
            // This will be updated with the DB-loaded trolley in InitializeApplicationAsync.
            TrolleyOperationsVM = new TrolleyOperationsViewModel(
                TrolleyVM, 
                tempTrolleyForOpsVm,
                _unitOfWorkFactory, // Pass the factory
                loggerFactory.CreateLogger<TrolleyOperationsViewModel>() // Create and pass the logger
            );
            TrolleyOperationsVM.SetMainViewModel(this);

            // Initialize TaskViewModel with all required dependencies
            TaskVM = new TaskViewModel(
                _unitOfWorkFactory.CreateUnitOfWork(), // Create a new UnitOfWork instance
                connectionManager,
                opcService,
                _dispatcherService,
                loggerFactory.CreateLogger<TaskViewModel>(),
                this
            );

            InitializeCommands();

            _logger.LogInformation("MainViewModel constructor END.");
        }

        #endregion
    }
}
