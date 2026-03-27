using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Production;
using RPGEconomy.Infrastructure.Persistence.Queries;
using System.Data;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class BuildingRepository : IBuildingRepository
{
    private readonly IDbConnectionFactory _factory;

    public BuildingRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<Building?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();

        var building = await conn.QueryFirstOrDefaultAsync<Building>(
            BuildingQueries.GetById, new { Id = id });

        if (building is null)
            return null;

        await LoadInputReservesAsync(conn, building);
        return building;
    }

    public async Task<IReadOnlyList<Building>> GetBySettlementIdAsync(int settlementId)
    {
        using var conn = _factory.Create();
        var result = (await conn.QueryAsync<Building>(
            BuildingQueries.GetBySettlementId, new { SettlementId = settlementId }))
            .ToList();

        foreach (var building in result)
            await LoadInputReservesAsync(conn, building);

        return result.AsReadOnly();
    }

    public async Task<int> SaveAsync(Building building)
    {
        using var conn = _factory.Create();

        int buildingId;

        if (building.IsNew)
        {
            buildingId = await conn.ExecuteScalarAsync<int>(
                BuildingQueries.Insert, new
                {
                    building.Name,
                    building.SettlementId,
                    building.RecipeId,
                    building.WorkerCount,
                    building.IsActive,
                    building.InputReserveCoverageTicks
                });
        }
        else
        {
            buildingId = building.Id;
            await conn.ExecuteAsync(BuildingQueries.Update, new
            {
                building.Id,
                building.Name,
                building.WorkerCount,
                building.IsActive,
                building.InputReserveCoverageTicks
            });
        }

        await conn.ExecuteAsync(
            BuildingQueries.DeleteInputReserves,
            new { BuildingId = buildingId });

        if (building.InputReserveItems.Count > 0)
        {
            await conn.ExecuteAsync(
                BuildingQueries.InsertInputReserve,
                building.InputReserveItems.Select(item => new
                {
                    BuildingId = buildingId,
                    item.ProductTypeId,
                    item.Quantity
                }));
        }

        return buildingId;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(BuildingQueries.Delete, new { Id = id });
    }

    private static async Task LoadInputReservesAsync(IDbConnection conn, Building building)
    {
        var items = await conn.QueryAsync<BuildingInputReserveItem>(
            BuildingQueries.GetInputReserves,
            new { BuildingId = building.Id });

        building.LoadInputReserveItems(items);
    }
}

