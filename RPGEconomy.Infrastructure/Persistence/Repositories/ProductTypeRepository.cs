using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Infrastructure.Persistence.Queries;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class ProductTypeRepository : IProductTypeRepository
{
    private readonly IDbConnectionFactory _factory;

    public ProductTypeRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<ProductType?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<ProductType>(
            ProductTypeQueries.GetById, new { Id = id });
    }

    public async Task<IReadOnlyList<ProductType>> GetAllAsync()
    {
        using var conn = _factory.Create();
        var result = await conn.QueryAsync<ProductType>(ProductTypeQueries.GetAll);
        return result.ToList().AsReadOnly();
    }

    public async Task<ProductType?> GetByNameAsync(string name)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<ProductType>(
            ProductTypeQueries.GetByName, new { Name = name });
    }

    public async Task<int> SaveAsync(ProductType productType)
    {
        using var conn = _factory.Create();

        if (productType.IsNew)
            return await conn.ExecuteScalarAsync<int>(
                ProductTypeQueries.Insert, new
                {
                    productType.Name,
                    productType.Description,
                    productType.BasePrice,
                    productType.WeightPerUnit
                });

        await conn.ExecuteAsync(ProductTypeQueries.Update, new
        {
            productType.Id,
            productType.Name,
            productType.Description,
            productType.BasePrice,
            productType.WeightPerUnit
        });
        return productType.Id;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(ProductTypeQueries.Delete, new { Id = id });
    }
}
