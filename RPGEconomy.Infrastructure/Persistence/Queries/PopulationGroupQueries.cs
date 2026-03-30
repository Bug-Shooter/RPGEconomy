namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class PopulationGroupQueries
{
    public const string GetById = """
        SELECT id,
               settlement_id AS "settlementId",
               name,
               population_size AS "populationSize",
               reserve_coverage_ticks AS "reserveCoverageTicks"
        FROM population_groups
        WHERE id = @Id
        """;

    public const string GetBySettlementId = """
        SELECT id,
               settlement_id AS "settlementId",
               name,
               population_size AS "populationSize",
               reserve_coverage_ticks AS "reserveCoverageTicks"
        FROM population_groups
        WHERE settlement_id = @SettlementId
        ORDER BY id
        """;

    public const string Insert = """
        INSERT INTO population_groups (settlement_id, name, population_size, reserve_coverage_ticks)
        VALUES (@SettlementId, @Name, @PopulationSize, @ReserveCoverageTicks)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE population_groups
        SET name = @Name,
            population_size = @PopulationSize,
            reserve_coverage_ticks = @ReserveCoverageTicks
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM population_groups
        WHERE id = @Id
        """;

    public const string GetConsumptionProfile = """
        SELECT id, population_group_id AS "populationGroupId", product_type_id AS "productTypeId",
               amount_per_person_per_tick AS "amountPerPersonPerTick"
        FROM population_group_consumption
        WHERE population_group_id = @PopulationGroupId
        ORDER BY id
        """;

    public const string DeleteConsumptionProfile = """
        DELETE FROM population_group_consumption
        WHERE population_group_id = @PopulationGroupId
        """;

    public const string InsertConsumptionProfileItem = """
        INSERT INTO population_group_consumption
            (population_group_id, product_type_id, amount_per_person_per_tick)
        VALUES
            (@PopulationGroupId, @ProductTypeId, @AmountPerPersonPerTick)
        """;

    public const string GetStockItems = """
        SELECT id,
               population_group_id AS "populationGroupId",
               product_type_id AS "productTypeId",
               quantity
        FROM population_group_stocks
        WHERE population_group_id = @PopulationGroupId
        ORDER BY id
        """;

    public const string DeleteStockItems = """
        DELETE FROM population_group_stocks
        WHERE population_group_id = @PopulationGroupId
        """;

    public const string InsertStockItem = """
        INSERT INTO population_group_stocks
            (population_group_id, product_type_id, quantity)
        VALUES
            (@PopulationGroupId, @ProductTypeId, @Quantity)
        """;
}
