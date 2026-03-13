using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Infrastructure.Persistence.Queries;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly IDbConnectionFactory _factory;

    public CurrencyRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<Currency?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<Currency>(
            CurrencyQueries.GetById, new { Id = id });
    }

    public async Task<IReadOnlyList<Currency>> GetAllAsync()
    {
        using var conn = _factory.Create();
        var result = await conn.QueryAsync<Currency>(CurrencyQueries.GetAll);
        return result.ToList().AsReadOnly();
    }

    public async Task<Currency?> GetByCodeAsync(string code)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<Currency>(
            CurrencyQueries.GetByCode, new { Code = code });
    }

    public async Task<int> SaveAsync(Currency currency)
    {
        using var conn = _factory.Create();

        if (currency.IsNew)
            return await conn.ExecuteScalarAsync<int>(
                CurrencyQueries.Insert, new
                {
                    currency.Name,
                    currency.Code,
                    currency.ExchangeRateToBase
                });

        await conn.ExecuteAsync(CurrencyQueries.Update, new
        {
            currency.Id,
            currency.Name,
            currency.Code,
            currency.ExchangeRateToBase
        });
        return currency.Id;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(CurrencyQueries.Delete, new { Id = id });
    }
}
