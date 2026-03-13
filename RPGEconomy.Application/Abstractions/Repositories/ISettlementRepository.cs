using RPGEconomy.Domain.World;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface ISettlementRepository : IRepository<Settlement>
{
    Task<IReadOnlyList<Settlement>> GetByWorldIdAsync(int worldId);
}
