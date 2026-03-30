using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.World;
using RPGEconomy.Infrastructure.Persistence.Queries;
using System.Data;
using System.Transactions;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class SettlementRepository : ISettlementRepository
{
    private readonly IDbConnectionFactory _factory;

    public SettlementRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<Settlement?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<Settlement>(SettlementQueries.GetById, new { Id = id });
    }

    public async Task<IReadOnlyList<Settlement>> GetByWorldIdAsync(int worldId)
    {
        using var conn = _factory.Create();
        var result = await conn.QueryAsync<Settlement>(SettlementQueries.GetByWorldId, new { WorldId = worldId });
        return result.ToList().AsReadOnly();
    }

    public async Task<int> SaveAsync(Settlement settlement)
    {
        using var conn = _factory.Create();

        if (settlement.IsNew)
        {
            return await conn.ExecuteScalarAsync<int>(
                SettlementQueries.Insert,
                new
                {
                    settlement.WorldId,
                    settlement.Name
                });
        }

        await conn.ExecuteAsync(
            SettlementQueries.Update,
            new
            {
                settlement.Id,
                settlement.Name
            });
        return settlement.Id;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var useLocalTransaction = Transaction.Current is null;
        using var tx = useLocalTransaction ? conn.BeginTransaction() : null;
        await conn.ExecuteAsync(SettlementQueries.Delete, new { Id = id }, tx);
        tx?.Commit();
    }
}
