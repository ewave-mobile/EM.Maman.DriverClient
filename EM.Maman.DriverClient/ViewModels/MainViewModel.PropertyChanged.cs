using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EM.Maman.DriverClient.ViewModels
{
    // Partial class containing the INotifyPropertyChanged implementation for MainViewModel
    public partial class MainViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
