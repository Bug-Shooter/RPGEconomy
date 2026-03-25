using Microsoft.Extensions.Configuration;

namespace RPGEconomy.Infrastructure.Migrations;

public static class MigrationExtensions
{
    public static void RunDatabaseMigrations(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        var migrationRunner = new MigrationRunner(connectionString);
        migrationRunner.Run();
    }
}
