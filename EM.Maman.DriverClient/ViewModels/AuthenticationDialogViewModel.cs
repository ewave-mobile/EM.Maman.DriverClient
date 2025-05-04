using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows; // Required for Window

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// ViewModel for the Pallet Authentication Dialog.
    /// </summary>
    public class AuthenticationDialogViewModel : INotifyPropertyChanged
    {
        private PalletAuthenticationItem _itemToAuthenticate;
        private string _enteredUldCode;
        private ICommand _confirmCommand;
        private ICommand _cancelCommand;

        public PalletAuthenticationItem ItemToAuthenticate
        {
            get => _itemToAuthenticate;
            private set { _itemToAuthenticate = value; OnPropertyChanged(); } // Set privately
        }

        public string EnteredUldCode
        {
            get => _enteredUldCode;
            set { _enteredUldCode = value; OnPropertyChanged(); }
        }

        public ICommand ConfirmCommand => _confirmCommand ??= new RelayCommand(Confirm);
        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(Cancel);

        // Property to hold the result for the calling window
        public bool? DialogResult { get; private set; }

        public AuthenticationDialogViewModel(PalletAuthenticationItem itemToAuthenticate)
        {
            ItemToAuthenticate = itemToAuthenticate;
        }

        private void Confirm(object parameter)
        {
            // Basic validation: Ensure something was entered
            if (string.IsNullOrWhiteSpace(EnteredUldCode))
            {
                MessageBox.Show("Please enter the Pallet ID (פרט).", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Set result and close
            DialogResult = true;
            CloseDialog(parameter as Window);
        }

        private void Cancel(object parameter)
        {
            DialogResult = false;
            CloseDialog(parameter as Window);
        }

        private void CloseDialog(Window window)
        {
            if (window != null)
            {
                // The standard way to close a dialog shown with ShowDialog()
                // is handled by setting the DialogResult in the Window's code-behind
                // based on the ViewModel's DialogResult.
                // We don't directly close it here, but the calling code will use our DialogResult.
                // If this VM were used with a non-dialog window, window.Close() would be appropriate.
                window.DialogResult = this.DialogResult;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
