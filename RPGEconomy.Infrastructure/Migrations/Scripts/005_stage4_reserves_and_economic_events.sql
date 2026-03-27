ALTER TABLE population_groups
    ADD COLUMN reserve_coverage_ticks NUMERIC NOT NULL DEFAULT 0;

ALTER TABLE buildings
    ADD COLUMN input_reserve_coverage_ticks NUMERIC NOT NULL DEFAULT 0;

CREATE TABLE population_group_stocks (
    id                  SERIAL PRIMARY KEY,
    population_group_id INT     NOT NULL REFERENCES population_groups(id) ON DELETE CASCADE,
    product_type_id     INT     NOT NULL REFERENCES product_types(id),
    quantity            NUMERIC NOT NULL DEFAULT 0
);

CREATE TABLE building_input_reserves (
    id              SERIAL PRIMARY KEY,
    building_id     INT     NOT NULL REFERENCES buildings(id) ON DELETE CASCADE,
    product_type_id INT     NOT NULL REFERENCES product_types(id),
    quantity        NUMERIC NOT NULL DEFAULT 0
);

CREATE TABLE economic_events (
    id            SERIAL PRIMARY KEY,
    settlement_id INT          NOT NULL REFERENCES settlements(id) ON DELETE CASCADE,
    name          VARCHAR(200) NOT NULL,
    is_enabled    BOOLEAN      NOT NULL DEFAULT FALSE,
    start_day     INT          NOT NULL,
    end_day       INT          NULL
);

CREATE TABLE economic_effects (
    id                  SERIAL PRIMARY KEY,
    economic_event_id   INT     NOT NULL REFERENCES economic_events(id) ON DELETE CASCADE,
    effect_type         INT     NOT NULL,
    value               NUMERIC NOT NULL,
    population_group_id INT     NULL REFERENCES population_groups(id) ON DELETE CASCADE,
    product_type_id     INT     NULL REFERENCES product_types(id)
);
