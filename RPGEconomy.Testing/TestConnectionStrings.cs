using Npgsql;

namespace RPGEconomy.Testing;

public static class TestConnectionStrings
{
    public const string DatabaseName = "rpg_economy_tests";

    public static string TestDatabase =>
        "Host=localhost;Port=5432;Database=rpg_economy_tests;Username=RPGEconomy;Password=SuperSecurePassword;Enlist=true";

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
}
