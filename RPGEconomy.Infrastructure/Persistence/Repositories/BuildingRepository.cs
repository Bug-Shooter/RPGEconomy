using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Production;
using RPGEconomy.Infrastructure.Persistence.Queries;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class BuildingRepository : IBuildingRepository
{
    private readonly IDbConnectionFactory _factory;

    public BuildingRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<Building?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<Building>(
            BuildingQueries.GetById, new { Id = id });
    }

    public async Task<IReadOnlyList<Building>> GetBySettlementIdAsync(int settlementId)
    {
        using var conn = _factory.Create();
        var result = await conn.QueryAsync<Building>(
            BuildingQueries.GetBySettlementId, new { SettlementId = settlementId });
        return result.ToList().AsReadOnly();
    }

    public async Task<int> SaveAsync(Building building)
    {
        using var conn = _factory.Create();

        if (building.IsNew)
            return await conn.ExecuteScalarAsync<int>(
                BuildingQueries.Insert, new
                {
                    building.Name,
                    building.SettlementId,
                    building.RecipeId,
                    building.WorkerCount,
                    building.IsActive
                });

        await conn.ExecuteAsync(BuildingQueries.Update, new
        {
            building.Id,
            building.Name,
            building.WorkerCount,
            building.IsActive
        });
        return building.Id;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(BuildingQueries.Delete, new { Id = id });
    }
}

