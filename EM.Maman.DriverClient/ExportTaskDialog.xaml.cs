using EM.Maman.DriverClient.EventArgs;
using EM.Maman.DriverClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EM.Maman.DriverClient
{
    /// <summary>
    /// Interaction logic for ExportTaskDialog.xaml
    /// </summary>
    public partial class ExportTaskDialog : Window
    {
        public ExportTaskDialog()
        {
            InitializeComponent();
            Loaded += ExportTaskDialog_Loaded;
        }

        private void ExportTaskDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ExportTaskViewModel viewModel)
            {
                viewModel.RequestClose += ViewModel_RequestClose;
            }
        }

        private void ViewModel_RequestClose(object sender, DialogResultEventArgs e)
        {
            DialogResult = e.Result;
            Close();
        }
    }
}
