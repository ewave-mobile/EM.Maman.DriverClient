using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows; // Required for Window
using EM.Maman.Models.Enums; // Required for UpdateType

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// ViewModel for the Pallet Authentication Dialog.
    /// </summary>
    public class AuthenticationDialogViewModel : INotifyPropertyChanged
    {
        private PalletAuthenticationItem _itemToAuthenticate;
        private string _enteredUldCode; // This will now hold either ULD or AWB
        private ICommand _confirmCommand;
        private ICommand _cancelCommand;

        public PalletAuthenticationItem ItemToAuthenticate
        {
            get => _itemToAuthenticate;
            private set 
            { 
                _itemToAuthenticate = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DialogDisplayTitle));
                OnPropertyChanged(nameof(MainPromptText));
                OnPropertyChanged(nameof(InputFieldHint));
                OnPropertyChanged(nameof(PalletDisplayDetail1Label));
                OnPropertyChanged(nameof(PalletDisplayDetail1Value));
                OnPropertyChanged(nameof(PalletDisplayDetail2Value));
                OnPropertyChanged(nameof(PalletDisplayDetail3Value));
            }
        }

        public string EnteredUldCode // Renaming to EnteredAuthenticationCode might be clearer, but keeping for now to minimize breaking changes to XAML binding path if it's deeply used.
        {
            get => _enteredUldCode;
            set { _enteredUldCode = value; OnPropertyChanged(); }
        }

        public ICommand ConfirmCommand => _confirmCommand ??= new RelayCommand(Confirm);
        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(Cancel);

        // Property to hold the result for the calling window
        public bool? DialogResult { get; private set; }

        public bool IsNewTaskCreationAllowed { get; private set; }

        public AuthenticationDialogViewModel(PalletAuthenticationItem itemToAuthenticate)
        {
            ItemToAuthenticate = itemToAuthenticate;
            // Determine if new task creation is allowed based on the authentication context
            IsNewTaskCreationAllowed = itemToAuthenticate.AuthContextMode == AuthenticationContextMode.Storage;
            // For Retrieval mode, new task creation is not allowed.
            // This property can be bound to the visibility/enabled state of a "Create New Task" button in the dialog.
        }

        private void Confirm(object parameter)
        {
            // Basic validation: Ensure something was entered
            if (string.IsNullOrWhiteSpace(EnteredUldCode))
            {
                string message = ItemToAuthenticate?.PalletDetails?.UpdateType == UpdateType.Export
                                 ? "יש להזין מספר שטר מטען (AWB)."
                                 : "יש להזין מספר פרט (ULD).";
                string caption = "נדרש קלט"; // "Input Required"
                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
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

        // New properties for dynamic UI text and details
        public string DialogDisplayTitle
        {
            get => ItemToAuthenticate?.PalletDetails?.UpdateType == UpdateType.Export
                   ? "אימות פלט יצוא לפני העמסה"
                   : "אימות פלט יבוא לפני העמסה";
        }

        public string MainPromptText
        {
            get => ItemToAuthenticate?.PalletDetails?.UpdateType == UpdateType.Export
                   ? "אימות פלט יצוא"
                   : "אימות פלט יבוא";
        }

        public string InputFieldHint
        {
            get => ItemToAuthenticate?.PalletDetails?.UpdateType == UpdateType.Export
                   ? "הכנס מספר שטר מטען (AWB)"
                   : "הכנס מספר פרט (ULD)";
        }

        public string PalletDisplayDetail1Label
        {
            get => ItemToAuthenticate?.PalletDetails?.UpdateType == UpdateType.Export ? "שטר מטען" : "פרט";
        }
        public string PalletDisplayDetail1Value
        {
            get => ItemToAuthenticate?.PalletDetails?.UpdateType == UpdateType.Export
                   ? ItemToAuthenticate?.PalletDetails?.ExportAwbNumber
                   : ItemToAuthenticate?.PalletDetails?.ImportUnit; 
        }

        public string PalletDisplayDetail2Value // Label "מופע" is static in XAML
        {
            get => ItemToAuthenticate?.PalletDetails?.UpdateType == UpdateType.Export
                   ? ItemToAuthenticate?.PalletDetails?.ExportAwbAppearance
                   : ItemToAuthenticate?.PalletDetails?.ImportAppearance;
        }
        
        // Assuming label "מצהר" remains for import, and changes to "אחסון" for export.
        // If the label in the dialog for the third detail should also be dynamic:
        public string PalletDisplayDetail3Label 
        {
            get => ItemToAuthenticate?.PalletDetails?.UpdateType == UpdateType.Export ? "אחסון" : "מצהר";
        }

        public string PalletDisplayDetail3Value
        {
            get => ItemToAuthenticate?.PalletDetails?.UpdateType == UpdateType.Export
                   ? ItemToAuthenticate?.PalletDetails?.ExportAwbStorage
                   : ItemToAuthenticate?.PalletDetails?.ImportManifest;
        }
    }
}
