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
        if (product is null) return Result<ProductTypeDto>.Failure($"Товар с Id {id} не найден");
        return Result<ProductTypeDto>.Success(ToDto(product));
    }

    public async Task<Result<IReadOnlyList<ProductTypeDto>>> GetAllAsync()
    {
        var products = await _repo.GetAllAsync();
        return Result<IReadOnlyList<ProductTypeDto>>.Success(
            products.Select(ToDto).ToList().AsReadOnly());
    }

    public async Task<Result<ProductTypeDto>> CreateAsync(
        string name, string description, double basePrice, double weightPerUnit)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<ProductTypeDto>.Failure("Название товара не может быть пустым");

        if (basePrice <= 0)
            return Result<ProductTypeDto>.Failure("Базовая цена должна быть больше нуля");

        var product = ProductType.Create(name, description, basePrice, weightPerUnit);
        var id = await _repo.SaveAsync(product);
        return Result<ProductTypeDto>.Success(ToDto(product) with { Id = id });
    }

    public async Task<Result<ProductTypeDto>> UpdateAsync(
        int id, string name, string description, double basePrice, double weightPerUnit)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<ProductTypeDto>.Failure("Название товара не может быть пустым");

        if (basePrice <= 0)
            return Result<ProductTypeDto>.Failure("Базовая цена должна быть больше нуля");

        var product = await _repo.GetByIdAsync(id);
        if (product is null) return Result<ProductTypeDto>.Failure($"Товар с Id {id} не найден");

        product.Update(name, description, basePrice, weightPerUnit);
        await _repo.SaveAsync(product);
        return Result<ProductTypeDto>.Success(ToDto(product));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product is null) return Result.Failure($"Товар с Id {id} не найден");

        await _repo.DeleteAsync(id);
        return Result.Success();
    }

    private static ProductTypeDto ToDto(ProductType p) =>
        new(p.Id, p.Name, p.Description, p.BasePrice, p.WeightPerUnit);
}
