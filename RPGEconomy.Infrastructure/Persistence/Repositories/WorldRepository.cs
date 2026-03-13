using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.World;
using RPGEconomy.Infrastructure.Persistence.Queries;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class WorldRepository : IWorldRepository
{
    private readonly IDbConnectionFactory _factory;

    public WorldRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<World?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<World>(
            WorldQueries.GetById, new { Id = id });
    }

    public async Task<IReadOnlyList<World>> GetAllAsync()
    {
        using var conn = _factory.Create();
        var result = await conn.QueryAsync<World>(WorldQueries.GetAll);
        return result.ToList().AsReadOnly();
    }

    public async Task<int> SaveAsync(World world)
    {
        using var conn = _factory.Create();

        if (world.IsNew)
            return await conn.ExecuteScalarAsync<int>(WorldQueries.Insert, new
            {
                world.Name,
                world.Description,
                CurrentDay = 0
            });

        await conn.ExecuteAsync(WorldQueries.Update, new
        {
            world.Id,
            world.Name,
            world.Description,
            world.CurrentDay
        });
        return world.Id;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(WorldQueries.Delete, new { Id = id });
    }
}

