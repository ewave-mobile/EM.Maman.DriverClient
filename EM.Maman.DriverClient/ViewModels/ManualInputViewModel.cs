using EM.Maman.DAL;
using EM.Maman.DriverClient.EventArgs;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Enums;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    public class ManualInputViewModel : INotifyPropertyChanged
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private TaskDetails _taskDetails;
        private bool _isImportSelected = true;
        private string _importManifest;
        private string _importUnit;
        private string _importAppearance;
        private StorageTypeEnum _selectedStorageType = StorageTypeEnum.REG;
        private CargoType _selectedCargoType = CargoType.ULD;
        private HeightType _selectedHeightType = HeightType.LOW;
        private string _exportAwbNumber;
        private string _exportSwbPrefix;
        private string _exportAwbAppearance;
        private string _exportAwbStorage;
        private ObservableCollection<Finger> _fingers;
        private Finger _selectedSourceFinger;
        private Finger _selectedDestinationFinger;
        private bool _isBusy;
        private string _statusMessage;
        private RelayCommand _saveCommand;
        private RelayCommand _cancelCommand;
        private RelayCommand _switchToImportCommand;
        private RelayCommand _switchToExportCommand;

        public TaskDetails TaskDetails
        {
            get => _taskDetails;
            set
            {
                if (_taskDetails != value)
                {
                    _taskDetails = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsImportSelected
        {
            get => _isImportSelected;
            set
            {
                if (_isImportSelected != value)
                {
                    _isImportSelected = value;
                    OnPropertyChanged();
                    UpdateTaskDetails();
                }
            }
        }

        public string ImportManifest
        {
            get => _importManifest;
            set
            {
                if (_importManifest != value)
                {
                    _importManifest = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ImportUnit
        {
            get => _importUnit;
            set
            {
                if (_importUnit != value)
                {
                    _importUnit = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ImportAppearance
        {
            get => _importAppearance;
            set
            {
                if (_importAppearance != value)
                {
                    _importAppearance = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public StorageTypeEnum SelectedStorageType
        {
            get => _selectedStorageType;
            set
            {
                if (_selectedStorageType != value)
                {
                    _selectedStorageType = value;
                    OnPropertyChanged();
                }
            }
        }

        public CargoType SelectedCargoType
        {
            get => _selectedCargoType;
            set
            {
                if (_selectedCargoType != value)
                {
                    _selectedCargoType = value;
                    OnPropertyChanged();
                }
            }
        }

        public HeightType SelectedHeightType
        {
            get => _selectedHeightType;
            set
            {
                if (_selectedHeightType != value)
                {
                    _selectedHeightType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ExportAwbNumber
        {
            get => _exportAwbNumber;
            set
            {
                if (_exportAwbNumber != value)
                {
                    _exportAwbNumber = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ExportSwbPrefix
        {
            get => _exportSwbPrefix;
            set
            {
                if (_exportSwbPrefix != value)
                {
                    _exportSwbPrefix = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ExportAwbAppearance
        {
            get => _exportAwbAppearance;
            set
            {
                if (_exportAwbAppearance != value)
                {
                    _exportAwbAppearance = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ExportAwbStorage
        {
            get => _exportAwbStorage;
            set
            {
                if (_exportAwbStorage != value)
                {
                    _exportAwbStorage = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<Finger> Fingers
        {
            get => _fingers;
            set
            {
                if (_fingers != value)
                {
                    _fingers = value;
                    OnPropertyChanged();
                }
            }
        }

        public Finger SelectedSourceFinger
        {
            get => _selectedSourceFinger;
            set
            {
                if (_selectedSourceFinger != value)
                {
                    _selectedSourceFinger = value;
                    if (TaskDetails != null)
                    {
                        TaskDetails.SourceFinger = value;
                    }
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public Finger SelectedDestinationFinger
        {
            get => _selectedDestinationFinger;
            set
            {
                if (_selectedDestinationFinger != value)
                {
                    _selectedDestinationFinger = value;
                    if (TaskDetails != null)
                    {
                        TaskDetails.DestinationFinger = value;
                    }
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveCommand => _saveCommand ??= new RelayCommand(_ => Save(), _ => CanSave());
        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(_ => Cancel(), _ => true);
        public ICommand SwitchToImportCommand => _switchToImportCommand ??= new RelayCommand(_ => IsImportSelected = true);
        public ICommand SwitchToExportCommand => _switchToExportCommand ??= new RelayCommand(_ => IsImportSelected = false);

        // Event for dialog result
        public event EventHandler<DialogResultEventArgs> RequestClose;

        public ManualInputViewModel(IUnitOfWork unitOfWork)
        {
            // Get the UnitOfWorkFactory from the App's ServiceProvider
            _unitOfWorkFactory = (App.Current as App)?.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
            if (_unitOfWorkFactory == null)
            {
                throw new InvalidOperationException("Could not resolve IUnitOfWorkFactory from ServiceProvider");
            }

            Fingers = new ObservableCollection<Finger>();

            // Initialize a new task details object
            TaskDetails = new TaskDetails
            {
                TaskType = Models.Enums.TaskType.Storage, // Always set to Storage
                Status = Models.Enums.TaskStatus.Created,
                CreatedDateTime = DateTime.Now,
                Code = GenerateTaskCode(),
                // Set ReportType to HND
                // This will be set in the Pallet object
            };
        }

        public async System.Threading.Tasks.Task InitializeAsync()
        {
            await LoadFingersAsync();
        }

        private string GenerateTaskCode()
        {
            // Generate a unique code for the task
            string prefix = IsImportSelected ? "IMP" : "EXP";
            return $"{prefix}-{DateTime.Now:yyMMdd}-{new Random().Next(1000, 9999)}";
        }

        private async System.Threading.Tasks.Task LoadFingersAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading fingers...";

                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var fingersFromDb = await unitOfWork.Fingers.GetAllAsync(); // Fetch from DB
                    Fingers = new ObservableCollection<Finger>(fingersFromDb.OrderBy(f => f.Position));
                    StatusMessage = $"Loaded {Fingers.Count} fingers.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading fingers.";
                MessageBox.Show($"Error loading fingers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateTaskDetails()
        {
            // Update the task code based on the selected type
            TaskDetails.Code = GenerateTaskCode();
        }

        private void Save()
        {
            // Validate essential details before closing
            if (TaskDetails == null || string.IsNullOrWhiteSpace(TaskDetails.Code))
            {
                StatusMessage = "Task code is required.";
                return;
            }

            if (IsImportSelected)
            {
                // Validate import fields
                if (string.IsNullOrWhiteSpace(ImportUnit))
                {
                    StatusMessage = "Import Unit (פרט) is required.";
                    return;
                }

                if (SelectedSourceFinger == null)
                {
                    StatusMessage = "Source Finger must be selected.";
                    return;
                }
            }
            else
            {
                // Validate export fields
                if (string.IsNullOrWhiteSpace(ExportAwbNumber))
                {
                    StatusMessage = "Export AWB Number (שטר מטען) is required.";
                    return;
                }

                if (SelectedDestinationFinger == null)
                {
                    StatusMessage = "Destination Finger must be selected.";
                    return;
                }
            }

            // Create/Update Pallet object within TaskDetails
            TaskDetails.Pallet = CreatePalletFromInput();

            // Assign the selected finger
            if (IsImportSelected)
            {
                TaskDetails.SourceFinger = SelectedSourceFinger;
            }
            else
            {
                TaskDetails.DestinationFinger = SelectedDestinationFinger;
            }

            // Set a descriptive name
            if (IsImportSelected)
            {
                TaskDetails.Name = $"Import {ImportUnit} from {TaskDetails.SourceFinger?.DisplayName ?? "N/A"}";
            }
            else
            {
                TaskDetails.Name = $"Export {ExportAwbNumber} to {TaskDetails.DestinationFinger?.DisplayName ?? "N/A"}";
            }

            // Close the dialog, indicating success
            RequestClose?.Invoke(this, new DialogResultEventArgs(true));
        }

        private Pallet CreatePalletFromInput()
        {
            // Create a new pallet using the data from UI fields
            var pallet = new Pallet
            {
                Id = 0, // Explicitly set ID to 0 for a new pallet
                ReportType = ReportType.HND, // Always set to HND
                UpdateType = IsImportSelected ? UpdateType.Import : UpdateType.Export,
                CargoType = SelectedCargoType,
                StorageType = SelectedStorageType,
                HeightType = SelectedHeightType,
                DisplayName = IsImportSelected ? ImportUnit : ExportAwbNumber
            };

            if (IsImportSelected)
            {
                // Set import-specific fields
                pallet.ImportManifest = ImportManifest;
                pallet.ImportUnit = ImportUnit;
                pallet.ImportAppearance = ImportAppearance;
            }
            else
            {
                // Set export-specific fields
                pallet.ExportAwbNumber = ExportAwbNumber;
                pallet.ExportSwbPrefix = ExportSwbPrefix;
                pallet.ExportAwbAppearance = ExportAwbAppearance;
                pallet.ExportAwbStorage = ExportAwbStorage;
            }

            return pallet;
        }

        private void Cancel()
        {
            // Close the dialog with cancel result
            RequestClose?.Invoke(this, new DialogResultEventArgs(false));
        }

        private bool CanSave()
        {
            if (IsBusy)
                return false;

            if (IsImportSelected)
            {
                return !string.IsNullOrWhiteSpace(ImportUnit) && SelectedSourceFinger != null;
            }
            else
            {
                return !string.IsNullOrWhiteSpace(ExportAwbNumber) && SelectedDestinationFinger != null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Using the existing DialogResultEventArgs class from ImportTaskViewModel
}
