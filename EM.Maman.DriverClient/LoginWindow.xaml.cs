using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace EM.Maman.DriverClient
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public LoginWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            DataContext = this;
        }

        //string that binds to the wpf and write the current version number of the application with the word "Version"
        public string Version => $"גרסה {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
        public string Greeting => "hi";

        //event handler for the login button
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //change current window to main window using DI container
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            this.Close();
        }
    }
}