using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface ICurrencyRepository : IRepository<Currency>
{
    Task<IReadOnlyList<Currency>> GetAllAsync();
    Task<Currency?> GetByCodeAsync(string code);
}
