using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Infrastructure.Persistence;

namespace RPGEconomy.Infrastructure.Decorators;

public class TransactionSimulationDecorator : ISimulationEngine
{
    private readonly ISimulationEngine _inner;
    private readonly IDbConnectionFactory _factory;

    public TransactionSimulationDecorator(
        ISimulationEngine inner, IDbConnectionFactory factory)
    {
        _inner = inner;
        _factory = factory;
    }

    public async Task<Result<SimulationResultDto>> AdvanceAsync(int worldId, int days)
    {
        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();

        try
        {
            var result = await _inner.AdvanceAsync(worldId, days);
            tx.Commit();
            return result;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
