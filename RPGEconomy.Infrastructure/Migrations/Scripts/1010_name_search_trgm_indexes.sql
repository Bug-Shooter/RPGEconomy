CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS ix_product_types_name_trgm
    ON product_types
    USING GIN (name gin_trgm_ops);

CREATE INDEX IF NOT EXISTS ix_resource_types_name_trgm
    ON resource_types
    USING GIN (name gin_trgm_ops);

CREATE INDEX IF NOT EXISTS ix_production_recipes_name_trgm
    ON production_recipes
    USING GIN (name gin_trgm_ops);
