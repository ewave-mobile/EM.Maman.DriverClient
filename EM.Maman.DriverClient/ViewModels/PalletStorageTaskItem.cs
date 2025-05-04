using EM.Maman.Models.CustomModels; // For TaskDetails
using EM.Maman.Models.LocalDbModels;
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

        public Pallet PalletDetails
        {
            get => _palletDetails;
            set { _palletDetails = value; OnPropertyChanged(); }
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

        public PalletStorageTaskItem(Pallet pallet, TaskDetails storageTask)
        {
            PalletDetails = pallet;
            StorageTask = storageTask;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
