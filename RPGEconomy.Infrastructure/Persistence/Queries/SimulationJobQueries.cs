namespace RPGEconomy.Infrastructure.Persistence.Queries;

public static class SimulationJobQueries
{
    public const string GetById = """
        SELECT id, world_id AS WorldId, days, status, created_at_utc AS CreatedAtUtc,
               started_at_utc AS StartedAtUtc, completed_at_utc AS CompletedAtUtc, error
        FROM simulation_jobs
        WHERE id = @Id;
        """;

    public const string Insert = """
        INSERT INTO simulation_jobs (world_id, days, status, created_at_utc, started_at_utc, completed_at_utc, error)
        VALUES (@WorldId, @Days, @Status, @CreatedAtUtc, @StartedAtUtc, @CompletedAtUtc, @Error)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE simulation_jobs
        SET world_id = @WorldId,
            days = @Days,
            status = @Status,
            created_at_utc = @CreatedAtUtc,
            started_at_utc = @StartedAtUtc,
            completed_at_utc = @CompletedAtUtc,
            error = @Error
        WHERE id = @Id;
        """;

    public const string Delete = """
        DELETE FROM simulation_jobs
        WHERE id = @Id;
        """;
}
