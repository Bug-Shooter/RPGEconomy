namespace RPGEconomy.Domain.Markets;

internal static class MarketPricePolicy
{
    private const decimal Sensitivity = 0.1m;
    private const decimal MinPrice = 0.01m;
    private const decimal MaxTickChangeRatio = 0.5m;

    public static decimal Recalculate(decimal currentPrice, int supply, int demand)
    {
        if (currentPrice < MinPrice)
            currentPrice = MinPrice;

        if (supply == 0 && demand == 0)
            return currentPrice;

        if (supply == 0 && demand > 0)
            return ApplyFactor(currentPrice, 1m + Sensitivity);

        var ratio = (decimal)demand / supply;
        var rawFactor = 1m + Sensitivity * (ratio - 1m);
        var minFactor = 1m - MaxTickChangeRatio;
        var maxFactor = 1m + MaxTickChangeRatio;
        var clampedFactor = decimal.Clamp(rawFactor, minFactor, maxFactor);

        return ApplyFactor(currentPrice, clampedFactor);
    }

    private static decimal ApplyFactor(decimal currentPrice, decimal factor) =>
        decimal.Max(currentPrice * factor, MinPrice);
}
