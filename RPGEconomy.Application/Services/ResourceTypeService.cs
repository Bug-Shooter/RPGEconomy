using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Services;

public class ResourceTypeService : IResourceTypeService
{
    private readonly IResourceTypeRepository _repo;

    public ResourceTypeService(IResourceTypeRepository repo) => _repo = repo;

    public async Task<Result<ResourceTypeDto>> GetByIdAsync(int id)
    {
        var resource = await _repo.GetByIdAsync(id);
        if (resource is null)
            return Result<ResourceTypeDto>.Failure($"Тип ресурса с Id {id} не найден");

        return Result<ResourceTypeDto>.Success(ToDto(resource));
    }

    public async Task<Result<IReadOnlyList<ResourceTypeDto>>> GetAllAsync()
    {
        var resources = await _repo.GetAllAsync();
        return Result<IReadOnlyList<ResourceTypeDto>>.Success(
            resources.Select(ToDto).ToList().AsReadOnly());
    }

    public async Task<Result<ResourceTypeDto>> CreateAsync(
        string name, string description, bool isRenewable, double regenerationRatePerDay)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<ResourceTypeDto>.Failure("Название ресурса не может быть пустым");

        if (regenerationRatePerDay < 0)
            return Result<ResourceTypeDto>.Failure("Скорость восстановления не может быть отрицательной");

        var resource = ResourceType.Create(name, description, isRenewable, regenerationRatePerDay);
        var id = await _repo.SaveAsync(resource);
        return Result<ResourceTypeDto>.Success(ToDto(resource) with { Id = id });
    }

    public async Task<Result<ResourceTypeDto>> UpdateAsync(
        int id, string name, string description, bool isRenewable, double regenerationRatePerDay)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<ResourceTypeDto>.Failure("Название ресурса не может быть пустым");

        if (regenerationRatePerDay < 0)
            return Result<ResourceTypeDto>.Failure("Скорость восстановления не может быть отрицательной");

        var resource = await _repo.GetByIdAsync(id);
        if (resource is null)
            return Result<ResourceTypeDto>.Failure($"Тип ресурса с Id {id} не найден");

        resource.Update(name, description, isRenewable, regenerationRatePerDay);
        await _repo.SaveAsync(resource);
        return Result<ResourceTypeDto>.Success(ToDto(resource));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var resource = await _repo.GetByIdAsync(id);
        if (resource is null)
            return Result.Failure($"Тип ресурса с Id {id} не найден");

        await _repo.DeleteAsync(id);
        return Result.Success();
    }

    private static ResourceTypeDto ToDto(ResourceType r) =>
        new(r.Id, r.Name, r.Description, r.IsRenewable, r.RegenerationRatePerDay);
}
