using EM.Maman.DriverClient.ViewModels;
using EM.Maman.Models.CustomModels;
using System.Windows;

namespace EM.Maman.DriverClient
{
    /// <summary>
    /// Interaction logic for ManualTaskDialog.xaml
    /// </summary>
    public partial class ManualTaskDialog : Window
    {
        public TaskDetails TaskDetails { get; private set; }

        public ManualTaskDialog()
        {
            InitializeComponent();
            DataContext = new ManualTaskViewModel();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ManualTaskViewModel viewModel)
            {
                // Validate the input
                if (viewModel.Validate())
                {
                    // Set the task details
                    TaskDetails = viewModel.IsImportSelected ? viewModel.ImportVM.TaskDetails : viewModel.ExportVM.TaskDetails;
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
