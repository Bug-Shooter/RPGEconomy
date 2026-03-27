ALTER TABLE inventory_items
    ALTER COLUMN quantity TYPE NUMERIC USING quantity::NUMERIC;

ALTER TABLE recipe_ingredients
    ALTER COLUMN quantity TYPE NUMERIC USING quantity::NUMERIC;

ALTER TABLE market_offers
    ALTER COLUMN supply_volume TYPE NUMERIC USING supply_volume::NUMERIC,
    ALTER COLUMN demand_volume TYPE NUMERIC USING demand_volume::NUMERIC;

CREATE TABLE population_groups (
    id              SERIAL PRIMARY KEY,
    settlement_id   INT          NOT NULL REFERENCES settlements(id) ON DELETE CASCADE,
    name            VARCHAR(200) NOT NULL,
    population_size INT          NOT NULL DEFAULT 0
);

CREATE TABLE population_group_consumption (
    id                         SERIAL PRIMARY KEY,
    population_group_id        INT     NOT NULL REFERENCES population_groups(id) ON DELETE CASCADE,
    product_type_id            INT     NOT NULL,
    amount_per_person_per_tick NUMERIC NOT NULL DEFAULT 0
);
