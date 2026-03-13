using RPGEconomy.Domain.World;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IWorldRepository : IRepository<World>
{
    Task<IReadOnlyList<World>> GetAllAsync();
}
