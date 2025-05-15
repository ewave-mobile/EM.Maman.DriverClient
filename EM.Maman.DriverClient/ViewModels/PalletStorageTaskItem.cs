using EM.Maman.Models.CustomModels; // For TaskDetails
using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.Enums; // Added for UpdateType
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// Represents an authenticated pallet ready for its storage task.
    /// </summary>
    public class PalletStorageTaskItem : INotifyPropertyChanged
    {
        private Pallet _palletDetails;
        private TaskDetails _storageTask; // The newly created storage task
        private ICommand _goToStorageCommand;
        private ICommand _changeDestinationCommand;
        private object _dataContext; // Added for binding in CurrentTasksView.xaml
        private bool _canExecuteChangeDestinationCommand;

        public Pallet PalletDetails
        {
            get => _palletDetails;
            set 
            { 
                _palletDetails = value; 
                OnPropertyChanged(); 
                // Notify changes for all derived properties
                OnPropertyChanged(nameof(FullActionText));
                OnPropertyChanged(nameof(PalletColumn1Label));
                OnPropertyChanged(nameof(PalletColumn1Value));
                OnPropertyChanged(nameof(PalletColumn2Value)); 
                OnPropertyChanged(nameof(PalletColumn3Label));
                OnPropertyChanged(nameof(PalletColumn3Value));
            }
        }

        /// <summary>
        /// The newly created storage task associated with this authenticated pallet.
        /// </summary>
        public TaskDetails StorageTask
        {
            get => _storageTask;
            set { _storageTask = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Command to navigate the trolley to the storage destination.
        /// This will likely be handled by the MainViewModel.
        /// </summary>
        public ICommand GoToStorageCommand
        {
            get => _goToStorageCommand;
            set { _goToStorageCommand = value; OnPropertyChanged(); } // Allow setting from MainViewModel
        }

        /// <summary>
        /// Command to allow changing the destination (future implementation).
        /// This will likely be handled by the MainViewModel.
        /// </summary>
        public ICommand ChangeDestinationCommand
        {
            get => _changeDestinationCommand;
            set { _changeDestinationCommand = value; OnPropertyChanged(); } // Allow setting from MainViewModel
        }

        /// <summary>
        /// Gets or sets the DataContext for binding in XAML.
        /// </summary>
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets whether the ChangeDestinationCommand can be executed.
        /// This property is used for binding to the IsEnabled property of buttons.
        /// </summary>
        public bool CanExecuteChangeDestinationCommand
        {
            get => _canExecuteChangeDestinationCommand;
            set 
            { 
                if (_canExecuteChangeDestinationCommand != value)
                {
                    _canExecuteChangeDestinationCommand = value;
                    OnPropertyChanged();
                }
            }
        }

        public PalletStorageTaskItem(Pallet pallet, TaskDetails storageTask)
        {
            PalletDetails = pallet;
            StorageTask = storageTask;
            DataContext = this; // Set DataContext to self by default
            CanExecuteChangeDestinationCommand = false; // Default to disabled
        }

        // Properties for dynamic display based on PalletDetails.UpdateType
        public string FullActionText
        {
            get
            {
                if (PalletDetails?.UpdateType == UpdateType.Export)
                {
                    return "אחסון פלט - יצוא";
                }
                return "אחסון פלט - יבוא"; // Default or Import
            }
        }

        public string PalletColumn1Label
        {
            get => PalletDetails?.UpdateType == UpdateType.Export ? "שטר מטען" : "פרט";
        }

        public string PalletColumn1Value
        {
            get => PalletDetails?.UpdateType == UpdateType.Export ? PalletDetails?.ExportAwbNumber : PalletDetails?.ImportAppearance;
        }

        // Column 2 Label ("מופע") is static in XAML based on current plan.
        public string PalletColumn2Value
        {
            get => PalletDetails?.UpdateType == UpdateType.Export ? PalletDetails?.ExportAwbAppearance : PalletDetails?.ImportAppearance;
        }

        public string PalletColumn3Label
        {
            get => PalletDetails?.UpdateType == UpdateType.Export ? "אחסון" : "מצהר";
        }

        public string PalletColumn3Value
        {
            get => PalletDetails?.UpdateType == UpdateType.Export ? PalletDetails?.ExportAwbStorage : PalletDetails?.ImportManifest;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
