ALTER TABLE market_offers
    ALTER COLUMN current_price TYPE NUMERIC(18,4)
    USING ROUND(current_price::numeric, 4);

ALTER TABLE product_types
    ALTER COLUMN base_price TYPE NUMERIC(18,4)
    USING ROUND(base_price::numeric, 4);

ALTER TABLE currencies
    ALTER COLUMN exchange_rate_to_base TYPE NUMERIC(18,6)
    USING ROUND(exchange_rate_to_base::numeric, 6);
