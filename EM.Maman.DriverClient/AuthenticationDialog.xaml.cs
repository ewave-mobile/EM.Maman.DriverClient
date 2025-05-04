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
    /// Interaction logic for AuthenticationDialog.xaml
    /// </summary>
    public partial class AuthenticationDialog : Window
    {
        public AuthenticationDialog()
        {
            InitializeComponent();
            DataContextChanged += AuthenticationDialog_DataContextChanged;
        }

        private void AuthenticationDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is AuthenticationDialogViewModel viewModel)
            {
                // Focus the input box when the dialog loads and DataContext is set
                Loaded += (s, args) =>
                {
                    UldCodeTextBox.Focus();
                    Keyboard.Focus(UldCodeTextBox);
                };

                // Handle closing the dialog based on ViewModel result
                viewModel.PropertyChanged += (vmSender, vmArgs) =>
                {
                    if (vmArgs.PropertyName == nameof(AuthenticationDialogViewModel.DialogResult))
                    {
                        // Set the Window's DialogResult based on the ViewModel's property
                        // This will automatically close the dialog when shown with ShowDialog()
                        this.DialogResult = viewModel.DialogResult;
                    }
                };
            }
        }
    }
}
