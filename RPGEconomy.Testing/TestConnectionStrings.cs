using Npgsql;
using System.Text.Json;

namespace RPGEconomy.Testing;

public static class TestConnectionStrings
{
    private static readonly Lazy<string> DefaultConnectionString = new(LoadDefaultConnectionString);

    public static string TestDatabase => DefaultConnectionString.Value;

    public static string DatabaseName
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(TestDatabase);
            return builder.Database ?? throw new InvalidOperationException("Database name not specified");
        }
    }

    public static string AdminDatabase
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(TestDatabase)
            {
                Database = "postgres"
            };

            return builder.ConnectionString;
        }
    }

    public static string GetSettingsPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "appsettings.Test.json");
            if (File.Exists(candidate))
                return candidate;

            var repoCandidate = Path.Combine(current.FullName, "RPGEconomy.Testing", "appsettings.Test.json");
            if (File.Exists(repoCandidate))
                return repoCandidate;

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate appsettings.Test.json for test configuration.");
    }

    private static string LoadDefaultConnectionString()
    {
        var json = File.ReadAllText(GetSettingsPath());
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings) &&
            connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection) &&
            !string.IsNullOrWhiteSpace(defaultConnection.GetString()))
        {
            return defaultConnection.GetString()!;
        }

        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing in appsettings.Test.json.");
    }
}
