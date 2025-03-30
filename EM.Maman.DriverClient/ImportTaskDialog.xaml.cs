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
    /// Interaction logic for ImportTaskDialog.xaml
    /// </summary>
    public partial class ImportTaskDialog : Window
    {
        public ImportTaskDialog()
        {
            InitializeComponent();
            Loaded += ImportTaskDialog_Loaded;
        }

        private void ImportTaskDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ImportTaskViewModel viewModel)
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
