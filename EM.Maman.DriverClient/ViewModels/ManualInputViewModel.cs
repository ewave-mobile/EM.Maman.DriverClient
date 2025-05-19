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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Task = System.Threading.Tasks.Task;

namespace EM.Maman.DriverClient.ViewModels
{
    public class ManualInputViewModel : INotifyPropertyChanged
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly int _currentFingerId; // Added
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
        // private Finger _selectedSourceFinger; // Removed
        // private Finger _selectedDestinationFinger; // Removed
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

        public ManualInputViewModel(IUnitOfWorkFactory unitOfWorkFactory, int currentFingerId)
        {
            _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
            _currentFingerId = currentFingerId;

            if (_currentFingerId <= 0)
            {
                throw new ArgumentException("Valid Current Finger ID must be provided.", nameof(currentFingerId));
            }

            Fingers = new ObservableCollection<Finger>(); // Kept for potential other uses

            TaskDetails = new TaskDetails
            {
                TaskType = Models.Enums.TaskType.Storage, // Explicitly Storage
                Status = Models.Enums.TaskStatus.Created, // New Status
                CreatedDateTime = DateTime.Now,
                Code = GenerateTaskCode()
                // SourceFinger will be set in InitializeAsync or when currentFinger is loaded
            };
        }

        public async System.Threading.Tasks.Task InitializeAsync()
        {
            await LoadFingersAsync(); // This will set TaskDetails.SourceFinger
        }

        private string GenerateTaskCode()
        {
            // Generate a unique code for the task
            string prefix = IsImportSelected ? "IMP" : "EXP";
            return $"{prefix}-{DateTime.Now:yyMMdd}-{new Random().Next(1000, 9999)}";
        }

        private async Task LoadFingersAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading finger information..."; // General message
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var fingersFromDb = await unitOfWork.Fingers.GetAllAsync();
                    Fingers = new ObservableCollection<Finger>(fingersFromDb.OrderBy(f => f.Position));
                    var currentFingerEntity = Fingers.FirstOrDefault(f => f.Id == _currentFingerId);
                    if (currentFingerEntity != null && TaskDetails != null)
                    {
                        TaskDetails.SourceFinger = currentFingerEntity;
                    }
                    else if (_currentFingerId > 0 && TaskDetails != null) // Fallback if not in preloaded Fingers
                    {
                        // This case should ideally not happen if LoadFingersAsync is comprehensive
                        // or if _currentFingerId always matches an existing finger.
                        // For robustness, attempt to load it directly if TaskDetails.SourceFinger is still null.
                        var finger = await unitOfWork.Fingers.GetByIdAsync(_currentFingerId);
                        if (finger != null)
                        {
                            TaskDetails.SourceFinger = finger;
                        }
                        else
                        {
                             // Log or handle the case where the currentFingerId doesn't match any known finger
                        }
                    }
                    StatusMessage = $"Finger information loaded.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading finger data.";
                MessageBox.Show($"Error loading finger data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<long?> GetRandomAvailableCellIdAsync() // Return type changed to long?
        {
            try
            {
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var allCells = await unitOfWork.Cells.GetAllAsync();
                    if (!allCells.Any())
                    {
                        StatusMessage = "No cells available in the system.";
                        return null;
                    }

                    var palletsInCells = await unitOfWork.PalletInCells.GetAllAsync();
                    var occupiedCellIds = palletsInCells.Select(pic => pic.CellId).ToHashSet();
                    var availableCells = allCells.Where(c => !occupiedCellIds.Contains(c.Id)).ToList();

                    if (!availableCells.Any())
                    {
                        StatusMessage = "No available cells for storage.";
                        return null;
                    }

                    var random = new Random();
                    int randomIndex = random.Next(availableCells.Count);
                    return availableCells[randomIndex].Id; // Cell.Id is long
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error finding an available cell.";
                MessageBox.Show($"Error finding cell: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }


        private void UpdateTaskDetails()
        {
            TaskDetails.Code = GenerateTaskCode(); // Code generation depends on IsImportSelected
            TaskDetails.Pallet = CreatePalletFromInput(); // Pallet's UpdateType depends on IsImportSelected
        }

        private async void Save()
        {
            IsBusy = true;
            StatusMessage = "Saving task...";

            if (TaskDetails == null || string.IsNullOrWhiteSpace(TaskDetails.Code))
            {
                StatusMessage = "Task code is required.";
                IsBusy = false;
                return;
            }
             if (_currentFingerId <= 0)
            {
                StatusMessage = "Valid source finger not identified.";
                IsBusy = false;
                return;
            }


            if (IsImportSelected)
            {
                if (string.IsNullOrWhiteSpace(ImportUnit) || string.IsNullOrWhiteSpace(ImportManifest) || string.IsNullOrWhiteSpace(ImportAppearance))
                {
                    StatusMessage = "Import Manifest, Unit, and Appearance are required.";
                    IsBusy = false;
                    return;
                }
            }
            else // Export selected (data entry mode)
            {
                if (string.IsNullOrWhiteSpace(ExportAwbNumber) || string.IsNullOrWhiteSpace(ExportSwbPrefix))
                {
                    StatusMessage = "Export AWB Number and SWB Prefix are required.";
                    IsBusy = false;
                    return;
                }
            }
            
            TaskDetails.Pallet = CreatePalletFromInput(); // Ensure pallet is up-to-date
            TaskDetails.TaskType = Models.Enums.TaskType.Storage; // Explicitly Storage
            TaskDetails.Status = Models.Enums.TaskStatus.Created; // New Status
            
            // SourceFingerId is already set in constructor via TaskDetails.SourceFingerId
            // TaskDetails.SourceFingerId = _currentFingerId; // This is int?

            var randomDestinationCellId = await GetRandomAvailableCellIdAsync(); // This is long?
            if (randomDestinationCellId == null || randomDestinationCellId.Value <= 0)
            {
                // StatusMessage already set by GetRandomAvailableCellIdAsync
                IsBusy = false;
                return;
            }
            // TaskDetails.DestinationCellId = randomDestinationCellId.Value; // This is long?
            // Instead, load the Cell object and assign to TaskDetails.DestinationCell
            Cell destinationCellEntity = null;
            using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
            {
                destinationCellEntity = await uow.Cells.GetByIdAsync(randomDestinationCellId.Value);
            }

            if (destinationCellEntity == null)
            {
                StatusMessage = $"Could not load destination cell details for ID {randomDestinationCellId.Value}.";
                IsBusy = false;
                return;
            }
            TaskDetails.DestinationCell = destinationCellEntity;

            // For TaskDetails.Name, try to get DisplayNames
            string sourceFingerName = _currentFingerId.ToString();
            if (TaskDetails.SourceFinger != null) // If SourceFinger object was loaded
            {
                sourceFingerName = TaskDetails.SourceFinger.DisplayName;
            }
            else // Attempt to load it if only ID is present
            {
                using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var finger = await uow.Fingers.GetByIdAsync(_currentFingerId);
                    if (finger != null) sourceFingerName = finger.DisplayName;
                }
            }

            string destinationCellName = randomDestinationCellId.Value.ToString();
            using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
            {
                var cell = await uow.Cells.GetByIdAsync(randomDestinationCellId.Value);
                if (cell != null)
                {
                    destinationCellName = cell.DisplayName;
                    TaskDetails.DestinationCell = cell; // Also set the object if needed later
                }
            }
            
            string palletIdentifier = IsImportSelected ? $"{ImportManifest}-{ImportUnit}-{ImportAppearance}" : $"{ExportSwbPrefix}-{ExportAwbNumber}";
            TaskDetails.Name = $"Manual Storage for {palletIdentifier} from Finger {sourceFingerName} to Cell {destinationCellName}";

            IsBusy = false;
            StatusMessage = "Task details prepared.";
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
                // Only need pallet identifying fields now
                return !string.IsNullOrWhiteSpace(ImportUnit) && !string.IsNullOrWhiteSpace(ImportManifest) && !string.IsNullOrWhiteSpace(ImportAppearance) && _currentFingerId > 0;
            }
            else // Export data entry mode
            {
                return !string.IsNullOrWhiteSpace(ExportAwbNumber) && !string.IsNullOrWhiteSpace(ExportSwbPrefix) && _currentFingerId > 0;
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
