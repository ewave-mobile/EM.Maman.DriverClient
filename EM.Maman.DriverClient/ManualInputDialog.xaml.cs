using EM.Maman.DriverClient.EventArgs;
using EM.Maman.DriverClient.ViewModels;
using EM.Maman.Models.Interfaces;
using System.Windows;

namespace EM.Maman.DriverClient
{
    /// <summary>
    /// Interaction logic for ManualInputDialog.xaml
    /// </summary>
    public partial class ManualInputDialog : Window
    {
        private readonly ManualInputViewModel _viewModel;

        public ManualInputDialog(IUnitOfWork unitOfWork)
        {
            InitializeComponent();
            _viewModel = new ManualInputViewModel(unitOfWork);
            DataContext = _viewModel;

            // Subscribe to the RequestClose event
            _viewModel.RequestClose += ViewModel_RequestClose;

            // Initialize the view model asynchronously
            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }

        private void ViewModel_RequestClose(object sender, DialogResultEventArgs e)
        {
            // Set the dialog result based on the event args
            DialogResult = e.Result;
            Close();
        }
    }
}
