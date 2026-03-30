using Dapper;
using Npgsql;
using RPGEconomy.Infrastructure.Migrations;

namespace RPGEconomy.Testing;

public sealed class PostgresTestDatabase
{
    private static readonly string[] TablesToTruncate =
    [
        "economic_effects",
        "economic_events",
        "building_input_reserves",
        "population_group_stocks",
        "population_group_consumption",
        "population_groups",
        "inventory_items",
        "market_offers",
        "buildings",
        "warehouses",
        "markets",
        "recipe_ingredients",
        "production_recipes",
        "simulation_jobs",
        "settlements",
        "product_types",
        "resource_types",
        "currencies",
        "worlds"
    ];

    public static string ConnectionString => TestConnectionStrings.TestDatabase;

    public static async Task InitializeAsync()
    {
        await EnsureDatabaseExistsAsync();
        new MigrationRunner(ConnectionString).Run();
        await ResetAsync();
    }

    public static async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var truncateSql = $"TRUNCATE TABLE {string.Join(", ", TablesToTruncate)} RESTART IDENTITY CASCADE;";
        await conn.ExecuteAsync(truncateSql);
    }

    public static async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static async Task EnsureDatabaseExistsAsync()
    {
        await using var conn = new NpgsqlConnection(TestConnectionStrings.AdminDatabase);
        await conn.OpenAsync();

        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pg_database WHERE datname = @databaseName",
            new { databaseName = TestConnectionStrings.DatabaseName });

        if (exists == 0)
        {
            var command = $"CREATE DATABASE {TestConnectionStrings.DatabaseName}";
            await using var create = new NpgsqlCommand(command, conn);
            await create.ExecuteNonQueryAsync();
        }
    }
}
