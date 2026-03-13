-- 001_initial_schema.sql
CREATE TABLE worlds (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(200)    NOT NULL,
    description VARCHAR(1000)   NOT NULL,
    current_day INT             NOT NULL DEFAULT 0
);

CREATE TABLE settlements (
    id          SERIAL PRIMARY KEY,
    world_id    INT             NOT NULL REFERENCES worlds(id),
    name        VARCHAR(200)    NOT NULL,
    population  INT             NOT NULL DEFAULT 0
);

CREATE TABLE warehouses (
    id            SERIAL PRIMARY KEY,
    settlement_id INT NOT NULL REFERENCES settlements(id)
);

CREATE TABLE inventory_items (
    id              SERIAL PRIMARY KEY,
    warehouse_id    INT          NOT NULL REFERENCES warehouses(id),
    product_type_id INT          NOT NULL,
    quantity        INT          NOT NULL DEFAULT 0,
    quality         VARCHAR(50)  NOT NULL DEFAULT 'Normal'
);

CREATE TABLE markets (
    id            SERIAL PRIMARY KEY,
    settlement_id INT NOT NULL REFERENCES settlements(id)
);

CREATE TABLE market_offers (
    id              SERIAL PRIMARY KEY,
    market_id       INT     NOT NULL REFERENCES markets(id),
    product_type_id INT     NOT NULL,
    current_price   FLOAT   NOT NULL,
    supply_volume   INT     NOT NULL DEFAULT 0,
    demand_volume   INT     NOT NULL DEFAULT 0
);

CREATE TABLE product_types (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(200)  NOT NULL,
    description     VARCHAR(1000) NOT NULL,
    base_price      FLOAT         NOT NULL,
    weight_per_unit FLOAT         NOT NULL
);

CREATE TABLE production_recipes (
    id                  SERIAL PRIMARY KEY,
    name                VARCHAR(200) NOT NULL,
    labor_days_required FLOAT        NOT NULL
);

CREATE TABLE recipe_ingredients (
    id              SERIAL PRIMARY KEY,
    recipe_id       INT NOT NULL REFERENCES production_recipes(id),
    product_type_id INT NOT NULL,
    quantity        INT NOT NULL,
    is_input        BOOLEAN NOT NULL
);

CREATE TABLE buildings (
    id            SERIAL PRIMARY KEY,
    name          VARCHAR(200) NOT NULL,
    settlement_id INT          NOT NULL REFERENCES settlements(id),
    recipe_id     INT          NOT NULL REFERENCES production_recipes(id),
    worker_count  INT          NOT NULL DEFAULT 0,
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE resource_types (
    id                        SERIAL PRIMARY KEY,
    name                      VARCHAR(200)  NOT NULL,
    description               VARCHAR(1000) NOT NULL,
    is_renewable              BOOLEAN       NOT NULL DEFAULT FALSE,
    regeneration_rate_per_day FLOAT         NOT NULL DEFAULT 0
);

CREATE TABLE currencies (
    id                   SERIAL PRIMARY KEY,
    name                 VARCHAR(200) NOT NULL,
    code                 VARCHAR(10)  NOT NULL UNIQUE,
    exchange_rate_to_base FLOAT       NOT NULL DEFAULT 1.0
);
