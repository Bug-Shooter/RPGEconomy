using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Population;
using RPGEconomy.Infrastructure.Persistence.Queries;
using System.Data;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class PopulationGroupRepository : IPopulationGroupRepository
{
    private readonly IDbConnectionFactory _factory;

    public PopulationGroupRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<PopulationGroup?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();

        var group = await conn.QueryFirstOrDefaultAsync<PopulationGroup>(
            PopulationGroupQueries.GetById,
            new { Id = id });

        if (group is null)
            return null;

        await LoadChildrenAsync(conn, group);
        return group;
    }

    public async Task<IReadOnlyList<PopulationGroup>> GetBySettlementIdAsync(int settlementId)
    {
        using var conn = _factory.Create();

        var groups = (await conn.QueryAsync<PopulationGroup>(
            PopulationGroupQueries.GetBySettlementId,
            new { SettlementId = settlementId })).ToList();

        foreach (var group in groups)
            await LoadChildrenAsync(conn, group);

        return groups.AsReadOnly();
    }

    public async Task<int> SaveAsync(PopulationGroup entity)
    {
        using var conn = _factory.Create();

        int populationGroupId;
        if (entity.IsNew)
        {
            populationGroupId = await conn.ExecuteScalarAsync<int>(
                PopulationGroupQueries.Insert,
                new
                {
                    entity.SettlementId,
                    entity.Name,
                    entity.PopulationSize,
                    entity.ReserveCoverageTicks
                });
        }
        else
        {
            populationGroupId = entity.Id;
            await conn.ExecuteAsync(
                PopulationGroupQueries.Update,
                new
                {
                    entity.Id,
                    entity.Name,
                    entity.PopulationSize,
                    entity.ReserveCoverageTicks
                });
        }

        await conn.ExecuteAsync(
            PopulationGroupQueries.DeleteConsumptionProfile,
            new { PopulationGroupId = populationGroupId });

        if (entity.ConsumptionProfile.Count > 0)
        {
            await conn.ExecuteAsync(
                PopulationGroupQueries.InsertConsumptionProfileItem,
                entity.ConsumptionProfile.Select(item => new
                {
                    PopulationGroupId = populationGroupId,
                    item.ProductTypeId,
                    item.AmountPerPersonPerTick
                }));
        }

        await conn.ExecuteAsync(
            PopulationGroupQueries.DeleteStockItems,
            new { PopulationGroupId = populationGroupId });

        if (entity.StockItems.Count > 0)
        {
            await conn.ExecuteAsync(
                PopulationGroupQueries.InsertStockItem,
                entity.StockItems.Select(item => new
                {
                    PopulationGroupId = populationGroupId,
                    item.ProductTypeId,
                    item.Quantity
                }));
        }

        return populationGroupId;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync(
            PopulationGroupQueries.DeleteConsumptionProfile,
            new { PopulationGroupId = id },
            tx);
        await conn.ExecuteAsync(
            PopulationGroupQueries.DeleteStockItems,
            new { PopulationGroupId = id },
            tx);
        await conn.ExecuteAsync(
            PopulationGroupQueries.Delete,
            new { Id = id },
            tx);
        tx.Commit();
    }

    private static async Task LoadChildrenAsync(IDbConnection conn, PopulationGroup group)
    {
        var items = await conn.QueryAsync<ConsumptionProfileItem>(
            PopulationGroupQueries.GetConsumptionProfile,
            new { PopulationGroupId = group.Id });

        group.LoadConsumptionProfile(items);

        var stockItems = await conn.QueryAsync<PopulationStockItem>(
            PopulationGroupQueries.GetStockItems,
            new { PopulationGroupId = group.Id });

        group.LoadStockItems(stockItems);
    }
}
