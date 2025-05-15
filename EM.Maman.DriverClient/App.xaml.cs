using EM.Maman.BL.Managers;
using EM.Maman.DAL.Repositories;
using EM.Maman.DAL;
using EM.Maman.DriverClient.Services;
using EM.Maman.DriverClient.ViewModels;
using EM.Maman.DriverClient;
using EM.Maman.Models.Interfaces.Managers;
using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using EM.Maman.Services.PlcServices;
using EM.Maman.Services; // Already here for other services
using EM.Maman.Models.Dtos; // Required for LoginResultDto if used in ViewModels directly, though not strictly for DI setup
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using EM.Maman.DriverClient.Extensions;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using System.Linq;
using System.Net.Http;

namespace EM.Maman.DriverClient
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }
        private ILogger<App> _logger;
        public static bool IsFirstInitialization { get; private set; } = false; // Default to false

        public App()
        {
            // Set up global exception handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Set up basic file logging early to catch initialization errors
                ConfigureEarlyLogging();

                _logger.LogInformation("Application starting...");

                // Your existing configuration setup
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                Configuration = builder.Build();

                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                ServiceProvider = serviceCollection.BuildServiceProvider();

                // Replace the early logger with the configured one
                _logger = ServiceProvider.GetRequiredService<ILogger<App>>();

                // Database initialization with robust error handling
                await InitializeDatabase(); // This now sets IsFirstInitialization

                // Start with Login Window
                StartApplication();
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger?.LogCritical(ex, "Fatal error during application startup");

                // Write to a file directly as backup in case logging isn't configured
                try
                {
                    File.WriteAllText(
                        Path.Combine(Directory.GetCurrentDirectory(), "startup_error.log"),
                        $"[{DateTime.Now}] CRITICAL ERROR: {ex}");
                }
                catch
                {
                    // Best effort, ignore errors from file writing
                }

                // Show user-friendly message
                MessageBox.Show(
                    $"The application could not start due to an error:\n\n{ex.Message}\n\n" +
                    "Please contact support and provide the startup_error.log file.",
                    "Application Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Shutdown the application
                Current.Shutdown();
            }
        }

        private void ConfigureEarlyLogging()
        {
            // Set up minimal logging before full DI is available
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole()
                       .AddDebug()
                       .AddFile(Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log"));
            });

            _logger = loggerFactory.CreateLogger<App>();
        }

        // Mark method as async
        //private async System.Threading.Tasks.Task InitializeDatabase()
        //{
        //    try
        //    {
        //        _logger.LogInformation("Initializing database...");
        //        using (var scope = ServiceProvider.CreateScope())
        //        {
        //            var dbContext = scope.ServiceProvider.GetRequiredService<LocalMamanDBContext>();
        //            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>(); // Resolve UnitOfWork

        //            ApplyDatabaseMigrations(dbContext);

        //            // Check if configuration exists AFTER migration using the new repository
        //            try
        //            {
        //                _logger.LogInformation("Checking for existing configuration via repository...");
        //                // Use the new Configurations repository from UnitOfWork
        //                bool configurationExists = await unitOfWork.Configurations.AnyAsync();
        //                IsFirstInitialization = !configurationExists;
        //                _logger.LogInformation("IsFirstInitialization set to: {IsFirstInit}", IsFirstInitialization);
        //            }
        //            catch (Exception configEx)
        //            {
        //                _logger.LogError(configEx, "Error checking configuration table. Assuming not first initialization.");
        //                IsFirstInitialization = false; // Safer default if check fails
        //            }
        //        }
        //        _logger.LogInformation("Database initialization completed.");
        //    }
        //    catch (InvalidOperationException ex) when (ex.Message.Contains("pending changes"))
        //    {
        //        _logger.LogWarning(ex, "Database model has pending changes. Migration required.");

        //        MessageBox.Show(
        //            "The database schema needs to be updated. Please run the following commands in Package Manager Console:\n\n" +
        //            "1. Add-Migration UpdatedModel -Project \"EM.Maman.Models\" -StartupProject \"EM.Maman.DriverClient\"\n" +
        //            "2. Update-Database -Project \"EM.Maman.Models\" -StartupProject \"EM.Maman.DriverClient\"\n\n" +
        //            "The application will continue with limited functionality.",
        //            "Database Update Required",
        //            MessageBoxButton.OK,
        //            MessageBoxImage.Warning);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Database initialization error");

        //        MessageBox.Show(
        //            $"Database initialization error: {ex.Message}\n\n" +
        //            "The application will continue with limited functionality.",
        //            "Database Error",
        //            MessageBoxButton.OK,
        //            MessageBoxImage.Error);
        //    }
        //}

        private void StartApplication()
        {
            try
            {
                _logger.LogInformation("Loading main application window...");
                var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                loginWindow.Show();
                _logger.LogInformation("Application started successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading the application window");

                // Try with minimal window as fallback
                try
                {
                    _logger.LogInformation("Attempting to load minimal login window...");

                    // Create a minimal login window directly without using the full view model
                    var minimalWindow = new Window
                    {
                        Title = "Login (Minimal Mode)",
                        Width = 400,
                        Height = 300,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Content = new StackPanel
                        {
                            Margin = new Thickness(20),
                            Children =
                        {
                            new TextBlock
                            {
                                Text = "Application is running in minimal mode due to an error.",
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 0, 0, 20)
                            },
                            new TextBlock
                            {
                                Text = $"Error: {ex.Message}",
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 0, 0, 20)
                            },
                            new Button
                            {
                                Content = "Exit Application",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Padding = new Thickness(10, 5, 10, 5)
                            }
                        }
                        }
                    };

                    // Add click handler for the button
                    if (minimalWindow.Content is StackPanel panel &&
                        panel.Children.Count > 2 &&
                        panel.Children[2] is Button exitButton)
                    {
                        exitButton.Click += (s, e) => Current.Shutdown();
                    }

                    minimalWindow.Show();
                    _logger.LogInformation("Minimal login window loaded successfully.");
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogCritical(fallbackEx, "Critical error: Failed to load even minimal window");
                    MessageBox.Show(
                        "Fatal error: The application cannot start. Please check the logs for details.",
                        "Critical Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Current.Shutdown();
                }
            }
        }

        private void ApplyDatabaseMigrations(LocalMamanDBContext dbContext)
        {
            // Your existing migration code with added logging
            _logger.LogInformation("Applying database migrations...");
            dbContext.Database.Migrate();
            _logger.LogInformation("Database migrations applied successfully.");
        }

        #region Global Exception Handlers

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            _logger?.LogCritical(exception, "Unhandled AppDomain exception: {Message}", exception?.Message);

            LogToFile("unhandled_exception.log", exception?.ToString() ?? "Unknown exception");

            if (e.IsTerminating)
            {
                ShowFatalErrorMessage(exception);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "Unhandled UI thread exception: {Message}", e.Exception.Message);

            LogToFile("ui_exception.log", e.Exception.ToString());

            // For XAML parsing errors, we want to provide more specific guidance
            if (e.Exception is System.Windows.Markup.XamlParseException xamlEx)
            {
                MessageBox.Show(
                    $"XAML parsing error: {xamlEx.Message}\n\n" +
                    $"Line: {xamlEx.LineNumber}, Position: {xamlEx.LinePosition}\n\n" +
                    "This is likely due to an issue in the UI definition.",
                    "XAML Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(
                    $"An unexpected error occurred: {e.Exception.Message}\n\n" +
                    "The error has been logged. You may continue using the application, but some features might not work correctly.",
                    "Application Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            // Mark as handled to prevent application crash
            e.Handled = true;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "Unobserved task exception: {Message}", e.Exception.Message);

            LogToFile("task_exception.log", e.Exception.ToString());

            // Mark as observed to prevent application crash
            e.SetObserved();
        }

        private void ShowFatalErrorMessage(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    $"A fatal error has occurred: {ex?.Message ?? "Unknown error"}\n\n" +
                    "The application will now close. Please check the log files for details.",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // If we can't even show a message box, there's not much we can do
            }
        }

        private void LogToFile(string filename, string content)
        {
            try
            {
                var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logsDirectory))
                {
                    Directory.CreateDirectory(logsDirectory);
                }

                File.AppendAllText(
                    Path.Combine(logsDirectory, filename),
                    $"[{DateTime.Now}] {content}\n\n{new string('-', 80)}\n\n");
            }
            catch
            {
                // Best effort logging, ignore file writing errors
            }
        }

        #endregion

        protected override void OnExit(ExitEventArgs e)
        {
            _logger?.LogInformation("Application shutting down...");
            // Cleanup code
            base.OnExit(e);
            _logger?.LogInformation("Application shutdown complete.");
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register configuration
            services.AddSingleton<IConfiguration>(Configuration);

            // Add logging
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddDebug();
                var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logsDirectory))
                {
                    Directory.CreateDirectory(logsDirectory);
                }
                configure.AddFile(Path.Combine(logsDirectory, "app_{0:yyyy-MM-dd}.log"));
            });

            // IMPORTANT: Register DbContextFactory as Singleton
            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContextFactory<LocalMamanDBContext>(options =>
                options.UseSqlServer(connectionString)
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine), ServiceLifetime.Singleton);

            // Register a scoped DbContext for backward compatibility
            services.AddScoped<LocalMamanDBContext>(provider =>
            {
                var factory = provider.GetRequiredService<IDbContextFactory<LocalMamanDBContext>>();
                return factory.CreateDbContext();
            });

            // Register the UnitOfWork factory
            services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

            // Register UnitOfWork as Transient - each request gets a new instance
            services.AddTransient<IUnitOfWork>(provider =>
            {
                var factory = provider.GetRequiredService<IDbContextFactory<LocalMamanDBContext>>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                return new UnitOfWork(factory, loggerFactory.CreateLogger<UnitOfWork>());
            });

            // OPC Service (Singleton is fine for this)
            bool useSimulation = Configuration.GetValue<bool>("AppSettings:UseSimulationMode");
            if (useSimulation)
            {
                services.AddSingleton<IOpcService, SimulatedOpcService>();
            }
            else
            {
                services.AddSingleton<IOpcService, OpcUaService>();
            }

            // Register HttpClient as a singleton
            services.AddSingleton<HttpClient>();

            // Register Services
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            services.AddSingleton<ISynchronizationService, SynchronizationService>();

            // Register our new custom services
            services.AddTransient<IDBLoggerService, DBLoggerService>();
            services.AddTransient<IMamanHttpService, MamanHttpService>();

            // Register Business Layer Services
            services.AddScoped<IUserManager, UserManager>();

            // Note: Repositories are now created by UnitOfWork, so we don't need to register them individually
            // unless they're used independently of UnitOfWork

            // Register ViewModels - Modified to use the new pattern
            services.AddTransient<TrolleyViewModel>();
            services.AddTransient<WarehouseViewModel>();

            services.AddTransient<MainViewModel>(provider =>
            {
                return new MainViewModel(
                    provider.GetRequiredService<IOpcService>(),
                    provider.GetRequiredService<IUnitOfWorkFactory>(),
                    provider.GetRequiredService<IConnectionManager>(),
                    provider.GetRequiredService<IDispatcherService>(),
                    provider.GetRequiredService<ILoggerFactory>(),
                    provider.GetRequiredService<TrolleyViewModel>(),
                    provider.GetRequiredService<IConfiguration>()
                );
            });

            services.AddTransient<LoginViewModel>();
            services.AddTransient<TaskViewModel>();
            services.AddTransient<ImportTaskViewModel>();
            services.AddTransient<ExportTaskViewModel>();

            services.AddTransient<ManualTaskViewModel>(provider =>
            {
                return new ManualTaskViewModel(
                    provider.GetRequiredService<IUnitOfWork>(), // Keep for backward compatibility
                    provider.GetRequiredService<ImportTaskViewModel>(),
                    provider.GetRequiredService<ExportTaskViewModel>()
                );
            });

            // ManualInputViewModel and ManualInputDialog are now created directly with the appropriate parameters

            // Register Views
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<ManualTaskDialog>();
        }

        // Updated initialization method to use factory
        private async System.Threading.Tasks.Task InitializeDatabase()
        {
            try
            {
                _logger.LogInformation("Initializing database...");

                // Use the factory to create a short-lived context
                var dbContextFactory = ServiceProvider.GetRequiredService<IDbContextFactory<LocalMamanDBContext>>();

                using (var dbContext = await dbContextFactory.CreateDbContextAsync())
                {
                    ApplyDatabaseMigrations(dbContext);
                }

                // Create another context for the configuration check
                using (var dbContext = await dbContextFactory.CreateDbContextAsync())
                {
                    try
                    {
                        _logger.LogInformation("Checking for existing configuration...");
                        bool configurationExists = await dbContext.Configurations.AnyAsync();
                        IsFirstInitialization = !configurationExists;
                        _logger.LogInformation("IsFirstInitialization set to: {IsFirstInit}", IsFirstInitialization);
                    }
                    catch (Exception configEx)
                    {
                        _logger.LogError(configEx, "Error checking configuration table.");
                        IsFirstInitialization = false;
                    }
                }

                _logger.LogInformation("Database initialization completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization error");
                // Handle errors...
            }
        }
    }
}
