namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class PopulationGroupQueries
{
    public const string GetById = """
        SELECT id, settlement_id AS "settlementId", name, population_size AS "populationSize"
        FROM population_groups
        WHERE id = @Id
        """;

    public const string GetBySettlementId = """
        SELECT id, settlement_id AS "settlementId", name, population_size AS "populationSize"
        FROM population_groups
        WHERE settlement_id = @SettlementId
        ORDER BY id
        """;

    public const string Insert = """
        INSERT INTO population_groups (settlement_id, name, population_size)
        VALUES (@SettlementId, @Name, @PopulationSize)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE population_groups
        SET name = @Name,
            population_size = @PopulationSize
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
}
