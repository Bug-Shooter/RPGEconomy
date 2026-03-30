using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IResourceTypeRepository : IRepository<ResourceType>
{
    Task<IReadOnlyList<ResourceType>> GetAllAsync();
    Task<IReadOnlyList<ResourceType>> SearchByNameAsync(string search);
    Task<ResourceType?> GetByNameAsync(string name);
}
