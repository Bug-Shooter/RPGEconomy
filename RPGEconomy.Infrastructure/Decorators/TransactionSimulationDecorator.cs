using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using System.Transactions;

namespace RPGEconomy.Infrastructure.Decorators;

public class TransactionSimulationDecorator : ISimulationExecutor
{
    private readonly ISimulationExecutor _inner;

    public TransactionSimulationDecorator(ISimulationExecutor inner)
        => _inner = inner;

    public async Task<Result<SimulationExecutionResult>> ExecuteAsync(
        SimulationExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled);

        var result = await _inner.ExecuteAsync(request, cancellationToken);

        scope.Complete();
        return result;
    }
}
