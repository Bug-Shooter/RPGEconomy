using RPGEconomy.Domain.Production;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<Warehouse?> GetBySettlementIdAsync(int settlementId);
}
