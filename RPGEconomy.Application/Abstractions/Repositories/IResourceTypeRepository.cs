using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IResourceTypeRepository : IRepository<ResourceType>
{
    Task<IReadOnlyList<ResourceType>> GetAllAsync();
    Task<ResourceType?> GetByNameAsync(string name);
}
