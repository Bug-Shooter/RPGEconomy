using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Events;
using RPGEconomy.Infrastructure.Persistence.Queries;
using System.Data;
using System.Transactions;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class EconomicEventRepository : IEconomicEventRepository
{
    private readonly IDbConnectionFactory _factory;

    public EconomicEventRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<EconomicEvent?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();

        var economicEvent = await conn.QueryFirstOrDefaultAsync<EconomicEvent>(EconomicEventQueries.GetById, new { Id = id });
        if (economicEvent is null)
            return null;

        await LoadEffectsAsync(conn, economicEvent);
        return economicEvent;
    }

    public async Task<IReadOnlyList<EconomicEvent>> GetBySettlementIdAsync(int settlementId)
    {
        using var conn = _factory.Create();

        var items = (await conn.QueryAsync<EconomicEvent>(
            EconomicEventQueries.GetBySettlementId,
            new { SettlementId = settlementId })).ToList();

        foreach (var item in items)
            await LoadEffectsAsync(conn, item);

        return items.AsReadOnly();
    }

    public async Task<int> SaveAsync(EconomicEvent entity)
    {
        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var useLocalTransaction = Transaction.Current is null;
        using var tx = useLocalTransaction ? conn.BeginTransaction() : null;

        int economicEventId;
        if (entity.IsNew)
        {
            economicEventId = await conn.ExecuteScalarAsync<int>(
                EconomicEventQueries.Insert,
                new
                {
                    entity.SettlementId,
                    entity.Name,
                    entity.IsEnabled,
                    entity.StartDay,
                    entity.EndDay
                },
                tx);
        }
        else
        {
            economicEventId = entity.Id;
            await conn.ExecuteAsync(
                EconomicEventQueries.Update,
                new
                {
                    entity.Id,
                    entity.Name,
                    entity.IsEnabled,
                    entity.StartDay,
                    entity.EndDay
                },
                tx);
        }

        await conn.ExecuteAsync(EconomicEventQueries.DeleteEffects, new { EconomicEventId = economicEventId }, tx);

        if (entity.Effects.Count > 0)
        {
            await conn.ExecuteAsync(
                EconomicEventQueries.InsertEffect,
                entity.Effects.Select(effect => new
                {
                    EconomicEventId = economicEventId,
                    effect.EffectType,
                    effect.Value,
                    effect.PopulationGroupId,
                    effect.ProductTypeId
                }),
                tx);
        }

        tx?.Commit();
        return economicEventId;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(EconomicEventQueries.Delete, new { Id = id });
    }

    private static async Task LoadEffectsAsync(IDbConnection conn, EconomicEvent economicEvent)
    {
        var effects = await conn.QueryAsync<EconomicEffect>(
            EconomicEventQueries.GetEffects,
            new { EconomicEventId = economicEvent.Id });
        economicEvent.LoadEffects(effects);
    }
}
