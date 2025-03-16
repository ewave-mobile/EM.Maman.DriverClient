using System;
using System.IO;
using System.Windows;
using EM.Maman.BL;
using EM.Maman.BL.Managers;
using EM.Maman.DAL;
using EM.Maman.DAL.Repositories;
using EM.Maman.DriverClient.Services;
using EM.Maman.DriverClient.ViewModels;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.Interfaces.Managers;
using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.LocalDbModels;
using EM.Maman.Services;
using EM.Maman.Services.PlcServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EM.Maman.DriverClient
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Initialize database - apply migrations if needed
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LocalMamanDBContext>();
                ApplyDatabaseMigrations(dbContext);
            }

            // Start with Login Window
            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        private void ApplyDatabaseMigrations(LocalMamanDBContext dbContext)
        {
            try
            {
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
                logger.LogError(ex, "An error occurred while migrating the database");
                MessageBox.Show($"Database initialization error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                // Configure file logging here if needed
            });

            // Register DbContext
            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<LocalMamanDBContext>(options =>
                options.UseSqlServer(connectionString), ServiceLifetime.Transient);

            // Register OPC Service
            bool useSimulation = Configuration.GetValue<bool>("AppSettings:UseSimulationMode");
            if (useSimulation)
            {
             //   services.AddSingleton<IOpcService, SimulatedOpcService>();
            }
            else
            {
                services.AddSingleton<IOpcService, OpcUaService>();
            }

            // Register Services
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            services.AddSingleton<ISynchronizationService, SynchronizationService>();

            // Register Business Layer Services
          //  services.AddScoped<ITrolleyManager, TrolleyManager>();
          //  services.AddScoped<ITaskManager, TaskManager>();
           services.AddScoped<IUserManager, UserManager>();

            // Register Data Access Layer
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITrolleyRepository, TrolleyRepository>();
            services.AddScoped<ICellRepository, CellRepository>();
            services.AddScoped<IFingerRepository, FingerRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IOperationRepository, OperationRepository>();

            // Register ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<TrolleyViewModel>();

            // Register Views
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cleanup
            base.OnExit(e);
        }
    }
}