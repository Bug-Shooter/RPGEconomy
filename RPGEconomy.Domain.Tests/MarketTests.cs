using FluentAssertions;
using RPGEconomy.Domain.Markets;

namespace RPGEconomy.Domain.Tests;

public class MarketTests
{
    [Fact]
    public void RegisterProduct_Should_Reject_Duplicate_Product()
    {
        // Тест на дупликацию товаров на маркете
        var market = Market.Create(1);
        market.RegisterProduct(10, 20);

        var result = market.RegisterProduct(10, 30);

        result.IsSuccess.Should().BeFalse();
        market.Offers.Should().ContainSingle();
    }

    [Fact]
    public void UpdateMarket_Should_Fail_When_Product_Is_Missing()
    {
        // Тест на обновление несуществующего товара
        var market = Market.Create(1);

        var result = market.UpdateMarket(10, 5, 7);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateMarket_Should_Recalculate_Price_And_Respect_Minimum_Floor()
    {
        //Тест пересчета цены
        var market = Market.Create(1);
        market.RegisterProduct(10, 0.02);

        market.UpdateMarket(10, 1000, 1);

        market.GetPrice(10).Should().BeApproximately(0.018002, 0.000001);
    }
}
