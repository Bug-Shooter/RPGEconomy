using FluentAssertions;
using RPGEconomy.Domain.Markets;

namespace RPGEconomy.Domain.Tests;

public class MarketTests
{
    [Fact]
    public void RegisterProduct_Should_Reject_Duplicate_Product()
    {
        var market = Market.Create(1);
        market.RegisterProduct(10, 20m);

        var result = market.RegisterProduct(10, 30m);

        result.IsSuccess.Should().BeFalse();
        market.Offers.Should().ContainSingle();
    }

    [Fact]
    public void RegisterProduct_Should_Reject_NonPositive_Initial_Price()
    {
        var market = Market.Create(1);

        var result = market.RegisterProduct(10, 0m);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateProductState_Should_Fail_When_Product_Is_Missing()
    {
        var market = Market.Create(1);

        var result = market.UpdateProductState(10, 5, 7);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateProductState_Should_Reject_Negative_Supply()
    {
        var market = Market.Create(1);
        market.RegisterProduct(10, 10m);

        var result = market.UpdateProductState(10, -1, 3);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateProductState_Should_Raise_Price_When_Demand_Exceeds_Supply()
    {
        var market = Market.Create(1);
        market.RegisterProduct(10, 10m);

        market.UpdateProductState(10, 10, 20);

        market.GetPrice(10).Should().BeGreaterThan(10m);
    }

    [Fact]
    public void UpdateProductState_Should_Lower_Price_When_Supply_Exceeds_Demand()
    {
        var market = Market.Create(1);
        market.RegisterProduct(10, 10m);

        market.UpdateProductState(10, 20, 10);

        market.GetPrice(10).Should().BeLessThan(10m);
    }

    [Fact]
    public void UpdateProductState_Should_Raise_Price_When_Supply_Is_Zero_And_Demand_Is_Positive()
    {
        var market = Market.Create(1);
        market.RegisterProduct(10, 10m);

        market.UpdateProductState(10, 0, 5);

        market.GetPrice(10).Should().Be(11m);
    }

    [Fact]
    public void UpdateProductState_Should_Keep_Price_Stable_When_Supply_And_Demand_Are_Zero()
    {
        var market = Market.Create(1);
        market.RegisterProduct(10, 10m);

        market.UpdateProductState(10, 0, 0);

        market.GetPrice(10).Should().Be(10m);
    }

    [Fact]
    public void UpdateProductState_Should_Respect_Minimum_Floor()
    {
        var market = Market.Create(1);
        market.RegisterProduct(10, 0.01m);

        market.UpdateProductState(10, 1000, 1);

        market.GetPrice(10).Should().Be(0.01m);
    }

    [Fact]
    public void UpdateProductState_Should_Clamp_Growth_Per_Tick()
    {
        var market = Market.Create(1);
        market.RegisterProduct(10, 10m);

        market.UpdateProductState(10, 1, 100);

        market.GetPrice(10).Should().Be(15m);
    }
}
