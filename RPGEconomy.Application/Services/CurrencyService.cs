using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Services;

public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository _repo;

    public CurrencyService(ICurrencyRepository repo) => _repo = repo;

    public async Task<Result<CurrencyDto>> GetByIdAsync(int id)
    {
        var currency = await _repo.GetByIdAsync(id);
        if (currency is null)
            return Result<CurrencyDto>.Failure($"Валюта с Id {id} не найдена");

        return Result<CurrencyDto>.Success(ToDto(currency));
    }

    public async Task<Result<IReadOnlyList<CurrencyDto>>> GetAllAsync()
    {
        var currencies = await _repo.GetAllAsync();
        return Result<IReadOnlyList<CurrencyDto>>.Success(
            currencies.Select(ToDto).ToList().AsReadOnly());
    }

    public async Task<Result<CurrencyDto>> CreateAsync(string name, string code, decimal exchangeRateToBase)
    {
        var createResult = Currency.Create(name, code, exchangeRateToBase);
        if (!createResult.IsSuccess)
            return Result<CurrencyDto>.Failure(createResult.Error!);

        var currency = createResult.Value!;
        var id = await _repo.SaveAsync(currency);
        return Result<CurrencyDto>.Success(ToDto(currency) with { Id = id });
    }

    public async Task<Result<CurrencyDto>> UpdateAsync(int id, string name, string code, decimal exchangeRateToBase)
    {
        var currency = await _repo.GetByIdAsync(id);
        if (currency is null)
            return Result<CurrencyDto>.Failure($"Валюта с Id {id} не найдена");

        var updateResult = currency.Update(name, code, exchangeRateToBase);
        if (!updateResult.IsSuccess)
            return Result<CurrencyDto>.Failure(updateResult.Error!);

        await _repo.SaveAsync(currency);
        return Result<CurrencyDto>.Success(ToDto(currency));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var currency = await _repo.GetByIdAsync(id);
        if (currency is null)
            return Result.Failure($"Валюта с Id {id} не найдена");

        await _repo.DeleteAsync(id);
        return Result.Success();
    }

    private static CurrencyDto ToDto(Currency currency) =>
        new(currency.Id, currency.Name, currency.Code, currency.ExchangeRateToBase);
}
