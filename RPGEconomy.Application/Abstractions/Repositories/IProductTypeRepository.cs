using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IProductTypeRepository : IRepository<ProductType>
{
    Task<IReadOnlyList<ProductType>> GetAllAsync();
    Task<ProductType?> GetByNameAsync(string name);
    Task<bool> IsInUseAsync(int id);
}
