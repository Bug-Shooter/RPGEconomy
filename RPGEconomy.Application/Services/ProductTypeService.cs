using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Services;

public class ProductTypeService : IProductTypeService
{
    private readonly IProductTypeRepository _repo;

    public ProductTypeService(IProductTypeRepository repo) => _repo = repo;

    public async Task<Result<ProductTypeDto>> GetByIdAsync(int id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product is null)
            return Result<ProductTypeDto>.Failure($"Товар с Id {id} не найден");

        return Result<ProductTypeDto>.Success(ToDto(product));
    }

    public async Task<Result<IReadOnlyList<ProductTypeDto>>> GetAllAsync()
    {
        var products = await _repo.GetAllAsync();
        return Result<IReadOnlyList<ProductTypeDto>>.Success(
            products.Select(ToDto).ToList().AsReadOnly());
    }

    public async Task<Result<ProductTypeDto>> CreateAsync(
        string name,
        string description,
        decimal basePrice,
        double weightPerUnit)
    {
        var createResult = ProductType.Create(name, description, basePrice, weightPerUnit);
        if (!createResult.IsSuccess)
            return Result<ProductTypeDto>.Failure(createResult.Error!);

        var product = createResult.Value!;
        var id = await _repo.SaveAsync(product);
        return Result<ProductTypeDto>.Success(ToDto(product) with { Id = id });
    }

    public async Task<Result<ProductTypeDto>> UpdateAsync(
        int id,
        string name,
        string description,
        decimal basePrice,
        double weightPerUnit)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product is null)
            return Result<ProductTypeDto>.Failure($"Товар с Id {id} не найден");

        var updateResult = product.Update(name, description, basePrice, weightPerUnit);
        if (!updateResult.IsSuccess)
            return Result<ProductTypeDto>.Failure(updateResult.Error!);

        await _repo.SaveAsync(product);
        return Result<ProductTypeDto>.Success(ToDto(product));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product is null)
            return Result.Failure($"Товар с Id {id} не найден");

        if (await _repo.IsInUseAsync(id))
        {
            return Result.Failure(
                "Нельзя удалить тип товара, пока он используется в рецептах, рынках, складах или профилях потребления");
        }

        await _repo.DeleteAsync(id);
        return Result.Success();
    }

    private static ProductTypeDto ToDto(ProductType product) =>
        new(product.Id, product.Name, product.Description, product.BasePrice, product.WeightPerUnit);
}
