using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EM.Maman.DriverClient.ViewModels
{
    // Partial class containing the INotifyPropertyChanged implementation for TrolleyOperationsViewModel
    public partial class TrolleyOperationsViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
