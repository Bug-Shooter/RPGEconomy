using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Application.Services;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Simulation;

namespace RPGEconomy.Application.Tests;

public class SimulationServiceTests
{
    [Fact]
    public async Task AdvanceAsync_Should_Complete_Job_On_Success()
    {
        var executor = new FakeSimulationExecutor(
            Result<SimulationExecutionResult>.Success(
                new SimulationExecutionResult(new SimulationResultDto(1, 0, 2, []))));
        var jobs = new SimulationJobRepositoryFake();
        var service = new SimulationService(executor, jobs);

        var result = await service.AdvanceAsync(new RunSimulationCommand(1, 2), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        jobs.Stored.Values.Should().ContainSingle(x => x.Status == SimulationJobStatus.Completed);
    }

    [Fact]
    public async Task AdvanceAsync_Should_Mark_Job_Failed_When_Executor_Returns_Failure()
    {
        var executor = new FakeSimulationExecutor(Result<SimulationExecutionResult>.Failure("broken"));
        var jobs = new SimulationJobRepositoryFake();
        var service = new SimulationService(executor, jobs);

        var result = await service.AdvanceAsync(new RunSimulationCommand(1, 2), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        jobs.Stored.Values.Should().ContainSingle(x => x.Status == SimulationJobStatus.Failed && x.Error == "broken");
    }

    [Fact]
    public async Task AdvanceAsync_Should_Save_Failed_Job_When_Executor_Throws()
    {
        var executor = new ThrowingSimulationExecutor();
        var jobs = new SimulationJobRepositoryFake();
        var service = new SimulationService(executor, jobs);

        var action = async () => await service.AdvanceAsync(new RunSimulationCommand(1, 2));

        await action.Should().ThrowAsync<InvalidOperationException>();
        jobs.Stored.Values.Should().ContainSingle(x => x.Status == SimulationJobStatus.Failed && x.Error == "boom");
    }

    private sealed class FakeSimulationExecutor : ISimulationExecutor
    {
        private readonly Result<SimulationExecutionResult> _result;

        public FakeSimulationExecutor(Result<SimulationExecutionResult> result)
            => _result = result;

        public Task<Result<SimulationExecutionResult>> ExecuteAsync(SimulationExecutionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class ThrowingSimulationExecutor : ISimulationExecutor
    {
        public Task<Result<SimulationExecutionResult>> ExecuteAsync(SimulationExecutionRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    private sealed class SimulationJobRepositoryFake : ISimulationJobRepository
    {
        public Dictionary<int, SimulationJob> Stored { get; } = [];
        private int _nextId = 1;

        public Task<SimulationJob?> GetByIdAsync(int id) => Task.FromResult(Stored.GetValueOrDefault(id));

        public Task<int> SaveAsync(SimulationJob entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            Stored[id] = Clone(entity, id);
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            Stored.Remove(id);
            return Task.CompletedTask;
        }

        private static SimulationJob Clone(SimulationJob entity, int id) =>
            new(
                id,
                entity.WorldId,
                entity.Days,
                entity.Status,
                entity.CreatedAtUtc,
                entity.StartedAtUtc,
                entity.CompletedAtUtc,
                entity.Error);
    }
}
