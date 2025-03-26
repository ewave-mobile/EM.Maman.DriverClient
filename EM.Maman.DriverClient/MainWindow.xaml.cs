using EM.Maman.DriverClient.ViewModels;
using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Services.PlcServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EM.Maman.DriverClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            IOpcService opcService = new OpcUaService();
            this.DataContext = new MainViewModel(opcService);
        }

    }
}