using Dapper;
using DbUp;
using Npgsql;

namespace RPGEconomy.Infrastructure.Migrations;

public class MigrationRunner
{
    private readonly string _connectionString;

    public MigrationRunner(string connectionString)
        => _connectionString = connectionString;

    public void Run()
    {
        EnsureDatabaseExists();

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(MigrationRunner).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
            throw new Exception("Миграция БД завершилась с ошибкой", result.Error);
    }

    private void EnsureDatabaseExists()
    {
        // Подключаемся к системной БД postgres, не к нашей
        var masterConnectionString = GetMasterConnectionString();

        using var conn = new NpgsqlConnection(masterConnectionString);
        conn.Open();

        var dbName = GetDatabaseName();

        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM pg_database WHERE datname = @DbName",
            new { DbName = dbName });

        if (exists == 0)
        {
            // Параметры нельзя использовать в CREATE DATABASE — только конкатенация
            conn.Execute($"CREATE DATABASE \"{dbName}\"");
        }
    }

    // Меняет базу в connection string на "postgres"
    private string GetMasterConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Database = "postgres"
        };
        return builder.ToString();
    }

    private string GetDatabaseName()
    {
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        return builder.Database ?? throw new InvalidOperationException("Database name not specified");
    }
}

