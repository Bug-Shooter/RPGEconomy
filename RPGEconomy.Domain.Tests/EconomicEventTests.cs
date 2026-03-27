using FluentAssertions;
using RPGEconomy.Domain.Events;

namespace RPGEconomy.Domain.Tests;

public class EconomicEventTests
{
    [Fact]
    public void GetActiveEffects_Should_Return_Effects_Within_Active_Window()
    {
        var economicEvent = EconomicEvent.Create(
            1,
            "Uncertainty",
            true,
            3,
            5,
            [(EconomicEffectType.DesiredReserveCoverageMultiplier, 2m, null, 10)]).Value!;

        economicEvent.GetActiveEffects(2).Should().BeEmpty();
        economicEvent.GetActiveEffects(3).Should().ContainSingle();
        economicEvent.GetActiveEffects(6).Should().BeEmpty();
    }

    [Fact]
    public void EconomicEffect_Create_Should_Reject_Producer_Effect_Targeted_To_Population_Group()
    {
        var result = EconomicEffect.Create(
            1,
            EconomicEffectType.ProducerReserveCoverageMultiplier,
            2m,
            5,
            10);

        result.IsSuccess.Should().BeFalse();
    }
}
