using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Infrastructure.Persistence;

namespace RPGEconomy.Infrastructure.Decorators;

public class TransactionSimulationDecorator : ISimulationExecutor
{
    private readonly ISimulationExecutor _inner;
    private readonly IDbConnectionFactory _factory;

    public TransactionSimulationDecorator(
        ISimulationExecutor inner, IDbConnectionFactory factory)
    {
        _inner = inner;
        _factory = factory;
    }

    public async Task<Result<SimulationExecutionResult>> ExecuteAsync(
        SimulationExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();

        try
        {
            var result = await _inner.ExecuteAsync(request, cancellationToken);
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
