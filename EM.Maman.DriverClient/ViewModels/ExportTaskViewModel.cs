using EM.Maman.DAL.Test;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    public class ExportTaskViewModel : INotifyPropertyChanged
    {
        private readonly IUnitOfWork _unitOfWork;
        private TaskDetails _taskDetails;
        private ObservableCollection<Pallet> _availablePallets;
        private ObservableCollection<Finger> _fingers;
        private Pallet _selectedPallet;
        private Finger _selectedDestinationFinger;
        private bool _isBusy;
        private string _statusMessage;
        private bool _isEditingPallet;
        private RelayCommand _saveCommand;
        private RelayCommand _cancelCommand;
        private RelayCommand _selectPalletCommand;
        private RelayCommand _editPalletCommand;

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

        public ObservableCollection<Pallet> AvailablePallets
        {
            get => _availablePallets;
            set
            {
                if (_availablePallets != value)
                {
                    _availablePallets = value;
                    OnPropertyChanged();
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

        public Pallet SelectedPallet
        {
            get => _selectedPallet;
            set
            {
                if (_selectedPallet != value)
                {
                    _selectedPallet = value;
                    if (value != null)
                    {
                        // Update the task details when a pallet is selected
                        if (TaskDetails != null)
                        {
                            TaskDetails.Pallet = value;

                            // Also update the editable pallet properties to reflect the selected pallet
                            UpdatePalletProperties(value);
                        }
                    }
                    OnPropertyChanged();
                    (_editPalletCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

        public bool IsEditingPallet
        {
            get => _isEditingPallet;
            set
            {
                if (_isEditingPallet != value)
                {
                    _isEditingPallet = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand SaveCommand => _saveCommand ??= new RelayCommand(_ => Save(), _ => CanSave());
        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(_ => Cancel(), _ => true);
        public ICommand SelectPalletCommand => _selectPalletCommand ??= new RelayCommand(_ => SelectPallet(), _ => !IsBusy);
        public ICommand EditPalletCommand => _editPalletCommand ??= new RelayCommand(_ => EditPallet(), _ => CanEditPallet());

        // Event for dialog result
        public event EventHandler<DialogResultEventArgs> RequestClose;

        public ExportTaskViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            AvailablePallets = new ObservableCollection<Pallet>();
            Fingers = new ObservableCollection<Finger>();

            // Initialize a new task details object
            TaskDetails = new TaskDetails
            {
                TaskType = Models.Enums.TaskType.Export,
                Status = Models.Enums.TaskStatus.Created,
                CreatedDateTime = DateTime.Now,
                Code = GenerateTaskCode(),

            };

            // Load data
            //LoadFingersAsync();
          //  LoadPalletsAsync(); // Keep loading pallets (assuming this might be correct or needs separate review)
        }
        public async System.Threading.Tasks.Task InitializeAsync()
        {
            await LoadFingersAsync();
            await LoadPalletsAsync();
        }
        private string GenerateTaskCode()
        {
            // Generate a unique code for the task - starting with EXP for export
            return $"EXP-{DateTime.Now:yyMMdd}-{new Random().Next(1000, 9999)}";
        }

        private async System.Threading.Tasks.Task LoadFingersAsync()
        {
            try
            {
                IsBusy = true; // Use IsBusy for feedback
                StatusMessage = "Loading fingers...";
                var fingersFromDb = await _unitOfWork.Fingers.GetAllAsync(); // Fetch from DB
                Fingers = new ObservableCollection<Finger>(fingersFromDb.OrderBy(f => f.Position));
                StatusMessage = $"Loaded {Fingers.Count} fingers.";
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                StatusMessage = "Error loading fingers.";
                // Consider showing a message box or logging
                System.Windows.MessageBox.Show($"Error loading fingers: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async System.Threading.Tasks.Task LoadPalletsAsync()
        {
            await System.Threading.Tasks.Task.Delay(100);
            AvailablePallets = new ObservableCollection<Pallet>(TestDatabase.Pallets.OrderBy(p => p.DisplayName));
        }

        private void UpdatePalletProperties(Pallet pallet)
        {
            if (pallet == null) return;

            // Update the editable properties based on the selected pallet
            PalletDisplayName = pallet.DisplayName;
            UldType = pallet.UldType;
            UldCode = pallet.UldCode;
            UldNumber = pallet.UldNumber;
            UldAirline = pallet.UldAirline;
            PalletDescription = pallet.Description;
            IsSecurePallet = pallet.IsSecure ?? false;
        }

        private void Save()
        {
            // Validate the task details and save
            if (TaskDetails == null || string.IsNullOrWhiteSpace(TaskDetails.Code))
            {
                StatusMessage = "Task code is required.";
                return;
            }

            if (TaskDetails.Pallet == null && SelectedPallet == null)
            {
                StatusMessage = "Please select a pallet for export.";
                return;
            }

            if (TaskDetails.DestinationFinger == null)
            {
                StatusMessage = "Please select a destination finger.";
                return;
            }

            // If we're editing the pallet, update its properties in the SelectedPallet object
            // Note: This doesn't save to DB here, TaskViewModel handles that.
            if (IsEditingPallet && SelectedPallet != null)
            {
                ApplyPalletChanges(SelectedPallet);
            }

            // Ensure the TaskDetails object has the correct Pallet and DestinationFinger
            TaskDetails.Pallet = SelectedPallet; // Assign the selected pallet
            TaskDetails.DestinationFinger = SelectedDestinationFinger; // Assign selected finger

            // We need to determine the SourceCell based on the SelectedPallet.
            // This requires querying the database (PalletInCell table) to find where the pallet currently is.
            // This logic should ideally be in TaskViewModel.SaveTaskToDatabase or called from there.
            // For now, we'll leave TaskDetails.SourceCell null and let TaskViewModel handle finding it.
            TaskDetails.SourceCell = null; // TaskViewModel needs to find this before saving the Task

            // Set a descriptive name
            TaskDetails.Name = $"Export {TaskDetails.Pallet?.DisplayName ?? "N/A"} to {TaskDetails.DestinationFinger?.DisplayName ?? "N/A"}";

            // Close the dialog, indicating success. TaskViewModel will handle the actual DB save.
            RequestClose?.Invoke(this, new DialogResultEventArgs(true));
        }

        private void ApplyPalletChanges(Pallet pallet)
        {
            // Apply changes made in the edit mode to the pallet
            pallet.DisplayName = PalletDisplayName;
            pallet.UldType = UldType;
            pallet.UldCode = UldCode;
            pallet.UldNumber = UldNumber;
            pallet.UldAirline = UldAirline;
            pallet.Description = PalletDescription;
            pallet.IsSecure = IsSecurePallet;

            // Ensure the task details has the updated pallet
            TaskDetails.Pallet = pallet;
        }

        private void SelectPallet()
        {
            // This method is a placeholder for showing a pallet selection dialog
            // For now, it's handled directly through the SelectedPallet property binding
        }

        private void EditPallet()
        {
            // Enable pallet editing mode
            IsEditingPallet = true;
        }

        private void Cancel()
        {
            // Close the dialog with cancel result
            RequestClose?.Invoke(this, new DialogResultEventArgs(false));
        }

        private bool CanSave()
        {
            return !IsBusy && TaskDetails != null && SelectedPallet != null &&
                  SelectedDestinationFinger != null &&
                  (!IsEditingPallet || IsValidPalletData());
        }

        private bool CanEditPallet()
        {
            return SelectedPallet != null && !IsEditingPallet;
        }

        private bool IsValidPalletData()
        {
            return !string.IsNullOrWhiteSpace(PalletDisplayName) &&
                   !string.IsNullOrWhiteSpace(UldType);
        }

        #region Pallet Properties
        // These properties would be bound to input fields in the dialog for editing
        private string _palletDisplayName;
        private string _uldType;
        private string _uldCode;
        private string _uldNumber;
        private string _uldAirline;
        private string _palletDescription;
        private bool _isSecurePallet;

        public string PalletDisplayName
        {
            get => _palletDisplayName;
            set
            {
                if (_palletDisplayName != value)
                {
                    _palletDisplayName = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string UldType
        {
            get => _uldType;
            set
            {
                if (_uldType != value)
                {
                    _uldType = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string UldCode
        {
            get => _uldCode;
            set
            {
                if (_uldCode != value)
                {
                    _uldCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UldNumber
        {
            get => _uldNumber;
            set
            {
                if (_uldNumber != value)
                {
                    _uldNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UldAirline
        {
            get => _uldAirline;
            set
            {
                if (_uldAirline != value)
                {
                    _uldAirline = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PalletDescription
        {
            get => _palletDescription;
            set
            {
                if (_palletDescription != value)
                {
                    _palletDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSecurePallet
        {
            get => _isSecurePallet;
            set
            {
                if (_isSecurePallet != value)
                {
                    _isSecurePallet = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
