using FluentAssertions;
using RPGEconomy.Domain.Production;

namespace RPGEconomy.Domain.Tests;

public class BuildingTests
{
    [Fact]
    public void Activate_Deactivate_Should_Enforce_Valid_State_Transitions()
    {
        // Тест двойной активации и двойной деактивации

        var building = new Building(1, "Mill", 1, 1, 3, true);

        var deactivate = building.Deactivate();
        deactivate.IsSuccess.Should().BeTrue();
        building.IsActive.Should().BeFalse();

        var deactivateAgain = building.Deactivate();
        deactivateAgain.IsSuccess.Should().BeFalse();
        building.IsActive.Should().BeFalse();

        var activate = building.Activate();
        activate.IsSuccess.Should().BeTrue();
        building.IsActive.Should().BeTrue();

        var activateAgain = building.Activate();
        activateAgain.IsSuccess.Should().BeFalse();
        building.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(5, 2, 2)]
    [InlineData(3, 0, 0)]
    public void BatchesPerDay_Should_Return_Expected_Value(
        int workerCount,
        double laborDaysPerBatch,
        int expected)
    {
        //Тест количества произведенных товаров, при 'n работников' и 'n трудо-дней на партию'

        var building = new Building(1, "Mill", 1, 1, workerCount, true);

        var result = building.BatchesPerDay(laborDaysPerBatch);

        result.Should().Be(expected);
    }
}
