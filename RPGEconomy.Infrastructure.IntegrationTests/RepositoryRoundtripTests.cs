using FluentAssertions;
using RPGEconomy.Domain.Simulation;
using RPGEconomy.Infrastructure.Persistence;
using RPGEconomy.Infrastructure.Persistence.Repositories;
using RPGEconomy.Testing;
using WorldEntity = RPGEconomy.Domain.World.World;

namespace RPGEconomy.Infrastructure.IntegrationTests;

[Collection(IntegrationTestCollection.Database)]
public class RepositoryRoundtripTests
{
    private readonly DatabaseFixture _fixture;

    public RepositoryRoundtripTests(DatabaseFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task WorldRepository_Should_Save_Update_And_Delete()
    {
        await Testing.PostgresTestDatabase.ResetAsync();
        var repository = new WorldRepository(new NpgsqlConnectionFactory(Testing.PostgresTestDatabase.ConnectionString));

        var createResult = WorldEntity.Create("Earth", "Desc");
        createResult.IsSuccess.Should().BeTrue();
        var worldId = await repository.SaveAsync(createResult.Value!);
        var stored = await repository.GetByIdAsync(worldId);

        stored.Should().NotBeNull();
        stored!.Update("Earth 2", "Updated");
        stored.AdvanceDays(3);
        await repository.SaveAsync(stored);

        var updated = await repository.GetByIdAsync(worldId);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Earth 2");
        updated.CurrentDay.Should().Be(3);

        await repository.DeleteAsync(worldId);
        (await repository.GetByIdAsync(worldId)).Should().BeNull();
    }

    [Fact]
    public async Task SimulationJobRepository_Should_Persist_Status_Transitions()
    {
        await Testing.PostgresTestDatabase.ResetAsync();
        var factory = new NpgsqlConnectionFactory(Testing.PostgresTestDatabase.ConnectionString);
        var worldRepository = new WorldRepository(factory);
        var jobRepository = new SimulationJobRepository(factory);
        var createResult = WorldEntity.Create("Earth", "Desc");
        createResult.IsSuccess.Should().BeTrue();
        var worldId = await worldRepository.SaveAsync(createResult.Value!);

        var jobId = await jobRepository.SaveAsync(SimulationJob.Create(worldId, 2));
        var job = await jobRepository.GetByIdAsync(jobId);
        job.Should().NotBeNull();

        job!.MarkRunning();
        await jobRepository.SaveAsync(job);
        job.MarkCompleted();
        await jobRepository.SaveAsync(job);

        var updated = await jobRepository.GetByIdAsync(jobId);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(SimulationJobStatus.Completed);
        updated.StartedAtUtc.Should().NotBeNull();
        updated.CompletedAtUtc.Should().NotBeNull();
    }
}
