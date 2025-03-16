using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DAL
{
    public class DatabaseMigrator
    {
        private readonly ILogger<DatabaseMigrator> _logger;

        public DatabaseMigrator(ILogger<DatabaseMigrator> logger)
        {
            _logger = logger;
        }

        public async System.Threading.Tasks.Task MigrateAsync(LocalMamanDBContext context)
        {
            try
            {
                _logger.LogInformation("Checking for database migrations");

                // Check if database exists
                bool dbExists = await context.Database.CanConnectAsync();

                if (!dbExists)
                {
                    _logger.LogInformation("Database does not exist. Creating the database");
                    await context.Database.EnsureCreatedAsync();
                    _logger.LogInformation("Database created successfully");
                }
                else
                {
                    // Apply any pending migrations
                    _logger.LogInformation("Applying pending migrations");
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

                    foreach (var migration in pendingMigrations)
                    {
                        _logger.LogInformation($"Applying migration: {migration}");
                    }

                    await context.Database.MigrateAsync();
                    _logger.LogInformation("All migrations applied successfully");
                }

                // Seed initial data if needed
                await SeedInitialDataAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during database migration");
                throw;
            }
        }

        private async System.Threading.Tasks.Task SeedInitialDataAsync(LocalMamanDBContext context)
        {
            try
            {
                // Check if seeding is needed
                if (!await context.Users.AnyAsync())
                {
                    _logger.LogInformation("Seeding initial user data");

                    // Add a default admin user
                    context.Users.Add(new User
                    {
                        Name = "Admin",
                        Code = "123456",
                        LastLoginDate = DateTime.Now
                    });

                    await context.SaveChangesAsync();
                    _logger.LogInformation("Initial user data seeded successfully");
                }

                // Add more seeding logic as needed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during data seeding");
                throw;
            }
        }
    }
}
