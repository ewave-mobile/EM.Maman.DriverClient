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
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }
        //string that binds to the wpf and write the current version number of the application with the word "Version"
        public string Version => $"גרסה {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
        public string Greeting => "hi";
        //event handler for the login button
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //change current window to main window
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
