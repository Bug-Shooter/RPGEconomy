using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Infrastructure.Persistence.Queries;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class ResourceTypeRepository : IResourceTypeRepository
{
    private readonly IDbConnectionFactory _factory;

    public ResourceTypeRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<ResourceType?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<ResourceType>(
            ResourceTypeQueries.GetById, new { Id = id });
    }

    public async Task<IReadOnlyList<ResourceType>> GetAllAsync()
    {
        using var conn = _factory.Create();
        var result = await conn.QueryAsync<ResourceType>(ResourceTypeQueries.GetAll);
        return result.ToList().AsReadOnly();
    }

    public async Task<ResourceType?> GetByNameAsync(string name)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<ResourceType>(
            ResourceTypeQueries.GetByName, new { Name = name });
    }

    public async Task<int> SaveAsync(ResourceType resourceType)
    {
        using var conn = _factory.Create();

        if (resourceType.IsNew)
            return await conn.ExecuteScalarAsync<int>(
                ResourceTypeQueries.Insert, new
                {
                    resourceType.Name,
                    resourceType.Description,
                    resourceType.IsRenewable,
                    resourceType.RegenerationRatePerDay
                });

        await conn.ExecuteAsync(ResourceTypeQueries.Update, new
        {
            resourceType.Id,
            resourceType.Name,
            resourceType.Description,
            resourceType.IsRenewable,
            resourceType.RegenerationRatePerDay
        });
        return resourceType.Id;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(ResourceTypeQueries.Delete, new { Id = id });
    }
}
