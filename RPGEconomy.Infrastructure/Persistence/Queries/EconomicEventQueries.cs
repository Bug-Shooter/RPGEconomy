namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class EconomicEventQueries
{
    public const string GetById = """
        SELECT id,
               settlement_id AS "settlementId",
               name,
               is_enabled AS "isEnabled",
               start_day AS "startDay",
               end_day AS "endDay"
        FROM economic_events
        WHERE id = @Id
        """;

    public const string GetBySettlementId = """
        SELECT id,
               settlement_id AS "settlementId",
               name,
               is_enabled AS "isEnabled",
               start_day AS "startDay",
               end_day AS "endDay"
        FROM economic_events
        WHERE settlement_id = @SettlementId
        ORDER BY id
        """;

    public const string GetActiveBySettlementId = """
        SELECT id,
               settlement_id AS "settlementId",
               name,
               is_enabled AS "isEnabled",
               start_day AS "startDay",
               end_day AS "endDay"
        FROM economic_events
        WHERE settlement_id = @SettlementId
          AND is_enabled = TRUE
          AND start_day <= @CurrentDay
          AND (end_day IS NULL OR end_day >= @CurrentDay)
        ORDER BY id
        """;

    public const string Insert = """
        INSERT INTO economic_events (settlement_id, name, is_enabled, start_day, end_day)
        VALUES (@SettlementId, @Name, @IsEnabled, @StartDay, @EndDay)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE economic_events
        SET name = @Name,
            is_enabled = @IsEnabled,
            start_day = @StartDay,
            end_day = @EndDay
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM economic_events
        WHERE id = @Id
        """;

    public const string GetEffects = """
        SELECT id,
               economic_event_id AS "economicEventId",
               effect_type AS "effectType",
               value,
               population_group_id AS "populationGroupId",
               product_type_id AS "productTypeId"
        FROM economic_effects
        WHERE economic_event_id = @EconomicEventId
        ORDER BY id
        """;

    public const string DeleteEffects = """
        DELETE FROM economic_effects
        WHERE economic_event_id = @EconomicEventId
        """;

    public const string InsertEffect = """
        INSERT INTO economic_effects (economic_event_id, effect_type, value, population_group_id, product_type_id)
        VALUES (@EconomicEventId, @EffectType, @Value, @PopulationGroupId, @ProductTypeId)
        """;
}
