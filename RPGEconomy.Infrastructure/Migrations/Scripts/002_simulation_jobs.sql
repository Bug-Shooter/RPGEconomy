CREATE TABLE simulation_jobs (
    id               SERIAL PRIMARY KEY,
    world_id         INT         NOT NULL REFERENCES worlds(id),
    days             INT         NOT NULL,
    status           INT         NOT NULL,
    created_at_utc   TIMESTAMPTZ NOT NULL,
    started_at_utc   TIMESTAMPTZ NULL,
    completed_at_utc TIMESTAMPTZ NULL,
    error            VARCHAR(1000) NULL
);
