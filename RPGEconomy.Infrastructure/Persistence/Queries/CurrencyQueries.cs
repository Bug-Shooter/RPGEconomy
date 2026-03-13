namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class CurrencyQueries
{
    public const string GetById = """
        SELECT id, name, code, exchange_rate_to_base AS "exchangeRateToBase"
        FROM currencies
        WHERE id = @Id
        """;

    public const string GetAll = """
        SELECT id, name, code, exchange_rate_to_base AS "exchangeRateToBase"
        FROM currencies
        """;

    public const string GetByCode = """
        SELECT id, name, code, exchange_rate_to_base AS "exchangeRateToBase"
        FROM currencies
        WHERE code = @Code
        """;

    public const string Insert = """
        INSERT INTO currencies (name, code, exchange_rate_to_base)
        VALUES (@Name, @Code, @ExchangeRateToBase)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE currencies
        SET name                 = @Name,
            code                 = @Code,
            exchange_rate_to_base = @ExchangeRateToBase
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM currencies WHERE id = @Id
        """;
}
