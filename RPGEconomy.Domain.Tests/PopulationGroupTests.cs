using FluentAssertions;
using RPGEconomy.Domain.Population;

namespace RPGEconomy.Domain.Tests;

public class PopulationGroupTests
{
    [Fact]
    public void Create_Should_Calculate_Demand_From_Profile_And_Population_Size()
    {
        var result = PopulationGroup.Create(
            1,
            "Peasants",
            50,
            [(10, 0.05m), (11, 1.2m)]);

        result.IsSuccess.Should().BeTrue();
        var demand = result.Value!.CalculateDemand();
        demand[10].Should().Be(2.50m);
        demand[11].Should().Be(60m);
    }

    [Fact]
    public void Create_Should_Reject_Duplicate_Product_In_Profile()
    {
        var result = PopulationGroup.Create(
            1,
            "Peasants",
            50,
            [(10, 0.05m), (10, 0.1m)]);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Update_Should_Reject_Negative_Population()
    {
        var group = PopulationGroup.Create(1, "Peasants", 10, []).Value!;

        var result = group.Update("Peasants", -1, []);

        result.IsSuccess.Should().BeFalse();
    }
}
