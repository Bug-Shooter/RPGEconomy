using Npgsql;
using System.Data;

namespace RPGEconomy.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString)
        => _connectionString = connectionString;

    public IDbConnection Create()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
