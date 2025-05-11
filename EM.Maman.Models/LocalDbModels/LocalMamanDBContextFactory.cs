using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using System.IO;

namespace EM.Maman.Models.LocalDbModels // Assuming LocalMamanDBContext is in this namespace
{
    public class LocalMamanDBContextFactory : IDesignTimeDbContextFactory<LocalMamanDBContext>
    {
        public LocalMamanDBContext CreateDbContext(string[] args)
        {
            // Get environment variable to determine the configuration file
            // Default to Development if not set, or adjust as needed for your project
            string environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Build configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                // Adjust the base path to navigate from EM.Maman.Models to the solution root, then to EM.Maman.DriverClient
                // This path might need adjustment based on where 'dotnet ef' is executed from.
                // Assuming 'dotnet ef' is run from the solution root or EM.Maman.DriverClient.
                // If EM.Maman.Models is the CWD for the tool, then ../EM.Maman.DriverClient
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../EM.Maman.DriverClient")) 
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<LocalMamanDBContext>();
            
            // Get connection string
            // Ensure you have a "DefaultConnection" or similar in your appsettings.json
            // Or use a specific design-time connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback or specific design-time string if DefaultConnection is not found
                // This is a common local DB connection string format. Adjust if yours is different.
                connectionString = "Data Source=(localdb)\\\\MSSQLLocalDB;Initial Catalog=LocalMamanDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
                System.Console.WriteLine("Warning: DefaultConnection not found in appsettings.json. Using fallback design-time connection string.");
            }
            
            optionsBuilder.UseSqlServer(connectionString);

            return new LocalMamanDBContext(optionsBuilder.Options);
        }
    }
}
