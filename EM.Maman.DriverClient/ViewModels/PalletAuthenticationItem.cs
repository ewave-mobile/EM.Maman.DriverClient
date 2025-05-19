using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.CustomModels; // Added for TaskDetails
using EM.Maman.Models.Enums; // Added for UpdateType
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    public enum AuthenticationContextMode
    {
        Storage,    // Standard authentication at a finger, allows new task creation if pallet not tied to existing task
        Retrieval   // Authentication at a source cell for a retrieval task, no new task creation allowed
    }

    /// <summary>
    /// Represents a pallet waiting for authentication.
    /// </summary>
    public class PalletAuthenticationItem : INotifyPropertyChanged
    {
        private Pallet _palletDetails;
        private AuthenticationContextMode _authContextMode;
        private TaskDetails _originalTask; // Task associated with this authentication attempt
        private ICommand _authenticateCommand;

        public AuthenticationContextMode AuthContextMode
        {
            get => _authContextMode;
            // Typically set at construction and not changed.
            // If it needs to be settable with INotifyPropertyChanged:
            // set { _authContextMode = value; OnPropertyChanged(); }
        }

        public Pallet PalletDetails
        {
            get => _palletDetails;
            set 
            { 
                _palletDetails = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayDetail1Label));
                OnPropertyChanged(nameof(DisplayDetail1Value));
                OnPropertyChanged(nameof(DisplayDetail2Value));
                OnPropertyChanged(nameof(DisplayDetail3Label));
                OnPropertyChanged(nameof(DisplayDetail3Value));
            }
        }

        public TaskDetails OriginalTask
        {
            get => _originalTask;
            set { _originalTask = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Command to initiate the authentication process for this pallet.
        /// This will likely be handled by the MainViewModel, passing this item as a parameter.
        /// </summary>
        public ICommand AuthenticateCommand
        {
            get => _authenticateCommand;
            set { _authenticateCommand = value; OnPropertyChanged(); } // Allow setting from MainViewModel
        }

        public PalletAuthenticationItem(Pallet pallet, TaskDetails originalTask)
        {
            PalletDetails = pallet;
            OriginalTask = originalTask;
            // Determine AuthContextMode based on the task type or how this item is created.
            // For now, let's assume it's passed in or defaulted.
            // Defaulting to Storage for existing logic. Will be set explicitly for Retrieval.
            _authContextMode = originalTask?.TaskType == Models.Enums.TaskType.Retrieval 
                ? AuthenticationContextMode.Retrieval 
                : AuthenticationContextMode.Storage;
        }

        // Constructor overload to explicitly set the mode if needed,
        // especially if an originalTask isn't always present or definitive at creation.
        public PalletAuthenticationItem(Pallet pallet, TaskDetails originalTask, AuthenticationContextMode authMode)
        {
            PalletDetails = pallet;
            OriginalTask = originalTask;
            _authContextMode = authMode;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // New dynamic properties for display
        public string DisplayDetail1Label
        {
            get => PalletDetails?.UpdateType == UpdateType.Export ? "שטר מטען" : "פרט";
        }
        public string DisplayDetail1Value
        {
            get => PalletDetails?.UpdateType == UpdateType.Export
                   ? PalletDetails?.ExportAwbNumber
                   : PalletDetails?.ImportUnit;
        }

        public string DisplayDetail2Value // Label "מופע" is static in XAML
        {
            get => PalletDetails?.UpdateType == UpdateType.Export
                   ? PalletDetails?.ExportAwbAppearance
                   : PalletDetails?.ImportAppearance;
        }

        public string DisplayDetail3Label
        {
            get => PalletDetails?.UpdateType == UpdateType.Export ? "אחסון" : "מצהר";
        }
        public string DisplayDetail3Value
        {
            get => PalletDetails?.UpdateType == UpdateType.Export
                   ? PalletDetails?.ExportAwbStorage
                   : PalletDetails?.ImportManifest;
        }
    }
}
