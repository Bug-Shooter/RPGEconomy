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

    public static Result<Currency> Create(string name, string code, decimal exchangeRateToBase)
    {
        var validation = Validate(name, code, exchangeRateToBase);
        if (!validation.IsSuccess)
            return Result<Currency>.Failure(validation.Error!);

        return Result<Currency>.Success(new Currency(0, name, code, exchangeRateToBase));
    }

    public Result Update(string name, string code, decimal exchangeRateToBase)
    {
        var validation = Validate(name, code, exchangeRateToBase);
        if (!validation.IsSuccess)
            return validation;

        Name = name;
        Code = code;
        ExchangeRateToBase = exchangeRateToBase;
        return Result.Success();
    }

    private static Result Validate(string name, string code, decimal exchangeRateToBase)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Название валюты не может быть пустым");

        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure("Код валюты не может быть пустым");

        if (exchangeRateToBase <= 0m)
            return Result.Failure("Курс обмена должен быть больше нуля");

        return Result.Success();
    }
}
