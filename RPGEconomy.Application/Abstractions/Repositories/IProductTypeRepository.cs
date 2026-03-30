using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IProductTypeRepository : IRepository<ProductType>
{
    Task<IReadOnlyList<ProductType>> GetAllAsync();
    Task<IReadOnlyList<ProductType>> SearchByNameAsync(string search);
    Task<ProductType?> GetByNameAsync(string name);
    Task<bool> IsInUseAsync(int id);
}
