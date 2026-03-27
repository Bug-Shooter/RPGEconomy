using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Resources;

public class Currency : AggregateRoot
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public decimal ExchangeRateToBase { get; private set; }

    // Dapper
    public Currency(int id, string name, string code, decimal exchangeRateToBase) : base(id)
    {
        Name = name;
        Code = code;
        ExchangeRateToBase = exchangeRateToBase;
    }

    public static Currency Create(string name, string code, decimal exchangeRateToBase)
        => new(0, name, code, exchangeRateToBase);

    public void Update(string name, string code, decimal exchangeRateToBase)
    {
        Name = name;
        Code = code;
        ExchangeRateToBase = exchangeRateToBase;
    }
}

