using FluentAssertions;
using RPGEconomy.Domain.Simulation;
using WorldEntity = RPGEconomy.Domain.World.World;

namespace RPGEconomy.Domain.Tests;

public class WorldAndSimulationJobTests
{
    [Fact]
    public void AdvanceDays_Should_Reject_NonPositive_Value()
    {
        var createResult = WorldEntity.Create("Earth", "Desc");
        createResult.IsSuccess.Should().BeTrue();

        var world = createResult.Value!;
        var result = world.AdvanceDays(0);

        result.IsSuccess.Should().BeFalse();
        world.CurrentDay.Should().Be(0);
    }

    [Fact]
    public void SimulationJob_Should_Track_Running_Completed_And_Error_Transitions()
    {
        var job = SimulationJob.Create(1, 3);

        var running = job.MarkRunning();
        var completed = job.MarkCompleted();
        var failAfterComplete = job.MarkFailed("boom");

        running.IsSuccess.Should().BeTrue();
        completed.IsSuccess.Should().BeTrue();
        failAfterComplete.IsSuccess.Should().BeFalse();
        job.StartedAtUtc.Should().NotBeNull();
        job.CompletedAtUtc.Should().NotBeNull();
        job.Error.Should().BeNull();
    }
}
