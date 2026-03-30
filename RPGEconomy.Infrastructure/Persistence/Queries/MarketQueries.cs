namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class MarketQueries
{
    public const string GetById = """
        SELECT id, settlement_id AS "settlementId"
        FROM markets
        WHERE id = @Id
        """;

    public const string GetBySettlementId = """
        SELECT id, settlement_id AS "settlementId"
        FROM markets
        WHERE settlement_id = @SettlementId
        """;

    public const string Insert = """
        INSERT INTO markets (settlement_id)
        VALUES (@SettlementId)
        RETURNING id;
        """;

    public const string GetOffers = """
        SELECT id, market_id AS "marketId", product_type_id AS "productTypeId", current_price AS "currentPrice", supply_volume AS "supplyVolume", demand_volume AS "demandVolume"
        FROM market_offers
        WHERE market_id = @MarketId
        """;

    public const string DeleteOffers = """
        DELETE FROM market_offers
        WHERE market_id = @MarketId
        """;

    public const string InsertOffer = """
        INSERT INTO market_offers
            (market_id, product_type_id, current_price, supply_volume, demand_volume)
        VALUES
            (@MarketId, @ProductTypeId, @CurrentPrice, @SupplyVolume, @DemandVolume)
        """;

    public const string Delete = """
        DELETE FROM markets
        WHERE id = @Id
        """;
}
