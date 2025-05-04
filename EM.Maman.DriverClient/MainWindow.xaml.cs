﻿using EM.Maman.DriverClient.ViewModels;
using System.Windows;
using Microsoft.Extensions.Logging; // Keep logging if needed for Loaded event

namespace EM.Maman.DriverClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly ILogger<MainWindow> _logger; // Keep logger if logging in Loaded

        // Constructor requires ILoggerFactory if logging is used
        public MainWindow(MainViewModel viewModel, ILoggerFactory loggerFactory)
        {
             _logger = loggerFactory.CreateLogger<MainWindow>();
            _logger.LogInformation("MainWindow constructor START");

            InitializeComponent(); // Assuming this compiles now

            _viewModel = viewModel;
            DataContext = _viewModel;

            // Add Loaded event handler
            this.Loaded += MainWindow_Loaded;

            _logger.LogInformation("MainWindow constructor END");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe to prevent running multiple times
            this.Loaded -= MainWindow_Loaded;

            _logger?.LogInformation("MainWindow Loaded. Starting async initialization...");
            // Consider showing a loading indicator UI element here

            // Call the async initialization for the main view model
            if (_viewModel != null)
            {
               await _viewModel.InitializeApplicationAsync();
            }
            else
            {
                _logger?.LogError("ViewModel was null in MainWindow_Loaded. Cannot initialize.");
            }

             _logger?.LogInformation("Async initialization complete.");
           // Consider hiding loading indicator here
        }
    }
}
