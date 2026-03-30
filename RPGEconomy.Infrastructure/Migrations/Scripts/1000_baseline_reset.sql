DROP TABLE IF EXISTS economic_effects CASCADE;
DROP TABLE IF EXISTS economic_events CASCADE;
DROP TABLE IF EXISTS building_input_reserves CASCADE;
DROP TABLE IF EXISTS buildings CASCADE;
DROP TABLE IF EXISTS population_group_stocks CASCADE;
DROP TABLE IF EXISTS population_group_consumption CASCADE;
DROP TABLE IF EXISTS population_groups CASCADE;
DROP TABLE IF EXISTS inventory_items CASCADE;
DROP TABLE IF EXISTS market_offers CASCADE;
DROP TABLE IF EXISTS warehouses CASCADE;
DROP TABLE IF EXISTS markets CASCADE;
DROP TABLE IF EXISTS recipe_ingredients CASCADE;
DROP TABLE IF EXISTS production_recipes CASCADE;
DROP TABLE IF EXISTS simulation_jobs CASCADE;
DROP TABLE IF EXISTS settlements CASCADE;
DROP TABLE IF EXISTS currencies CASCADE;
DROP TABLE IF EXISTS resource_types CASCADE;
DROP TABLE IF EXISTS product_types CASCADE;
DROP TABLE IF EXISTS worlds CASCADE;

CREATE TABLE worlds (
    id            SERIAL PRIMARY KEY,
    name          VARCHAR(200)  NOT NULL CHECK (btrim(name) <> ''),
    description   VARCHAR(1000) NOT NULL CHECK (btrim(description) <> ''),
    current_day   INT           NOT NULL DEFAULT 0 CHECK (current_day >= 0)
);

CREATE TABLE product_types (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(200)   NOT NULL UNIQUE CHECK (btrim(name) <> ''),
    description     VARCHAR(1000)  NOT NULL CHECK (btrim(description) <> ''),
    base_price      NUMERIC(18,4)  NOT NULL CHECK (base_price > 0),
    weight_per_unit DOUBLE PRECISION NOT NULL CHECK (weight_per_unit > 0)
);

CREATE TABLE resource_types (
    id                         SERIAL PRIMARY KEY,
    name                       VARCHAR(200)   NOT NULL UNIQUE CHECK (btrim(name) <> ''),
    description                VARCHAR(1000)  NOT NULL CHECK (btrim(description) <> ''),
    is_renewable               BOOLEAN        NOT NULL DEFAULT FALSE,
    regeneration_rate_per_day  DOUBLE PRECISION NOT NULL DEFAULT 0 CHECK (regeneration_rate_per_day >= 0)
);

CREATE TABLE currencies (
    id                     SERIAL PRIMARY KEY,
    name                   VARCHAR(200)  NOT NULL CHECK (btrim(name) <> ''),
    code                   VARCHAR(10)   NOT NULL UNIQUE CHECK (btrim(code) <> ''),
    exchange_rate_to_base  NUMERIC(18,6) NOT NULL DEFAULT 1 CHECK (exchange_rate_to_base > 0)
);

CREATE TABLE production_recipes (
    id                  SERIAL PRIMARY KEY,
    name                VARCHAR(200)      NOT NULL CHECK (btrim(name) <> ''),
    labor_days_required DOUBLE PRECISION  NOT NULL CHECK (labor_days_required > 0)
);

CREATE TABLE settlements (
    id          SERIAL PRIMARY KEY,
    world_id    INT          NOT NULL REFERENCES worlds(id) ON DELETE CASCADE,
    name        VARCHAR(200) NOT NULL CHECK (btrim(name) <> '')
);

CREATE TABLE warehouses (
    id            SERIAL PRIMARY KEY,
    settlement_id INT NOT NULL UNIQUE REFERENCES settlements(id) ON DELETE CASCADE
);

CREATE TABLE markets (
    id            SERIAL PRIMARY KEY,
    settlement_id INT NOT NULL UNIQUE REFERENCES settlements(id) ON DELETE CASCADE
);

CREATE TABLE market_offers (
    id              SERIAL PRIMARY KEY,
    market_id       INT           NOT NULL REFERENCES markets(id) ON DELETE CASCADE,
    product_type_id INT           NOT NULL REFERENCES product_types(id) ON DELETE RESTRICT,
    current_price   NUMERIC(18,4) NOT NULL CHECK (current_price > 0),
    supply_volume   NUMERIC(18,4) NOT NULL DEFAULT 0 CHECK (supply_volume >= 0),
    demand_volume   NUMERIC(18,4) NOT NULL DEFAULT 0 CHECK (demand_volume >= 0),
    CONSTRAINT uq_market_offers_market_product UNIQUE (market_id, product_type_id)
);

CREATE TABLE inventory_items (
    id              SERIAL PRIMARY KEY,
    warehouse_id    INT           NOT NULL REFERENCES warehouses(id) ON DELETE CASCADE,
    product_type_id INT           NOT NULL REFERENCES product_types(id) ON DELETE RESTRICT,
    quantity        NUMERIC(18,4) NOT NULL CHECK (quantity >= 0),
    quality         VARCHAR(50)   NOT NULL DEFAULT 'Normal',
    CONSTRAINT uq_inventory_items_warehouse_product_quality UNIQUE (warehouse_id, product_type_id, quality)
);

CREATE TABLE recipe_ingredients (
    id              SERIAL PRIMARY KEY,
    recipe_id       INT           NOT NULL REFERENCES production_recipes(id) ON DELETE CASCADE,
    product_type_id INT           NOT NULL REFERENCES product_types(id) ON DELETE RESTRICT,
    quantity        NUMERIC(18,4) NOT NULL CHECK (quantity > 0),
    is_input        BOOLEAN       NOT NULL,
    CONSTRAINT uq_recipe_ingredients_recipe_product_direction UNIQUE (recipe_id, product_type_id, is_input)
);

CREATE TABLE buildings (
    id                           SERIAL PRIMARY KEY,
    name                         VARCHAR(200)   NOT NULL CHECK (btrim(name) <> ''),
    settlement_id                INT            NOT NULL REFERENCES settlements(id) ON DELETE CASCADE,
    recipe_id                    INT            NOT NULL REFERENCES production_recipes(id) ON DELETE RESTRICT,
    worker_count                 INT            NOT NULL DEFAULT 0 CHECK (worker_count >= 0),
    is_active                    BOOLEAN        NOT NULL DEFAULT TRUE,
    input_reserve_coverage_ticks NUMERIC(18,4) NOT NULL DEFAULT 0 CHECK (input_reserve_coverage_ticks >= 0)
);

CREATE TABLE building_input_reserves (
    id              SERIAL PRIMARY KEY,
    building_id     INT           NOT NULL REFERENCES buildings(id) ON DELETE CASCADE,
    product_type_id INT           NOT NULL REFERENCES product_types(id) ON DELETE RESTRICT,
    quantity        NUMERIC(18,4) NOT NULL CHECK (quantity >= 0),
    CONSTRAINT uq_building_input_reserves_building_product UNIQUE (building_id, product_type_id)
);

CREATE TABLE population_groups (
    id                      SERIAL PRIMARY KEY,
    settlement_id           INT           NOT NULL REFERENCES settlements(id) ON DELETE CASCADE,
    name                    VARCHAR(200)  NOT NULL CHECK (btrim(name) <> ''),
    population_size         INT           NOT NULL DEFAULT 0 CHECK (population_size >= 0),
    reserve_coverage_ticks  NUMERIC(18,4) NOT NULL DEFAULT 0 CHECK (reserve_coverage_ticks >= 0)
);

CREATE TABLE population_group_consumption (
    id                         SERIAL PRIMARY KEY,
    population_group_id        INT           NOT NULL REFERENCES population_groups(id) ON DELETE CASCADE,
    product_type_id            INT           NOT NULL REFERENCES product_types(id) ON DELETE RESTRICT,
    amount_per_person_per_tick NUMERIC(18,4) NOT NULL CHECK (amount_per_person_per_tick >= 0),
    CONSTRAINT uq_population_group_consumption_group_product UNIQUE (population_group_id, product_type_id)
);

CREATE TABLE population_group_stocks (
    id                  SERIAL PRIMARY KEY,
    population_group_id INT           NOT NULL REFERENCES population_groups(id) ON DELETE CASCADE,
    product_type_id     INT           NOT NULL REFERENCES product_types(id) ON DELETE RESTRICT,
    quantity            NUMERIC(18,4) NOT NULL CHECK (quantity >= 0),
    CONSTRAINT uq_population_group_stocks_group_product UNIQUE (population_group_id, product_type_id)
);

CREATE TABLE economic_events (
    id            SERIAL PRIMARY KEY,
    settlement_id INT          NOT NULL REFERENCES settlements(id) ON DELETE CASCADE,
    name          VARCHAR(200) NOT NULL CHECK (btrim(name) <> ''),
    is_enabled    BOOLEAN      NOT NULL DEFAULT FALSE,
    start_day     INT          NOT NULL CHECK (start_day >= 0),
    end_day       INT          NULL,
    CONSTRAINT ck_economic_events_window CHECK (end_day IS NULL OR end_day >= start_day)
);

CREATE TABLE economic_effects (
    id                  SERIAL PRIMARY KEY,
    economic_event_id   INT           NOT NULL REFERENCES economic_events(id) ON DELETE CASCADE,
    effect_type         INT           NOT NULL CHECK (effect_type BETWEEN 1 AND 4),
    value               NUMERIC(18,4) NOT NULL CHECK (value > 0),
    population_group_id INT           NULL REFERENCES population_groups(id) ON DELETE CASCADE,
    product_type_id     INT           NULL REFERENCES product_types(id) ON DELETE RESTRICT,
    CONSTRAINT ck_economic_effects_scope
        CHECK (NOT (effect_type = 4 AND population_group_id IS NOT NULL))
);

CREATE UNIQUE INDEX ux_economic_effects_scope
    ON economic_effects (economic_event_id, effect_type, COALESCE(population_group_id, 0), COALESCE(product_type_id, 0));

CREATE TABLE simulation_jobs (
    id               SERIAL PRIMARY KEY,
    world_id         INT           NOT NULL REFERENCES worlds(id) ON DELETE CASCADE,
    days             INT           NOT NULL CHECK (days > 0),
    status           INT           NOT NULL,
    created_at_utc   TIMESTAMPTZ   NOT NULL,
    started_at_utc   TIMESTAMPTZ   NULL,
    completed_at_utc TIMESTAMPTZ   NULL,
    error            VARCHAR(1000) NULL
);
