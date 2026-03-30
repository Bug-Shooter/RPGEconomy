using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.World;

namespace RPGEconomy.Application.Services;

public class WorldService : IWorldService
{
    private readonly IWorldRepository _worldRepo;

    public WorldService(IWorldRepository worldRepo)
        => _worldRepo = worldRepo;

    public async Task<Result<WorldDto>> CreateAsync(string name, string description)
    {
        var createResult = World.Create(name, description);
        if (!createResult.IsSuccess)
            return Result<WorldDto>.Failure(createResult.Error!);

        var world = createResult.Value!;
        var id = await _worldRepo.SaveAsync(world);

        return Result<WorldDto>.Success(new WorldDto(id, world.Name, world.Description, world.CurrentDay));
    }

    public async Task<Result<WorldDto>> UpdateAsync(int id, string name, string description)
    {
        var world = await _worldRepo.GetByIdAsync(id);
        if (world is null)
            return Result<WorldDto>.Failure($"Мир с Id {id} не найден");

        var updateResult = world.Update(name, description);
        if (!updateResult.IsSuccess)
            return Result<WorldDto>.Failure(updateResult.Error!);

        await _worldRepo.SaveAsync(world);
        return Result<WorldDto>.Success(new WorldDto(world.Id, world.Name, world.Description, world.CurrentDay));
    }

    public async Task<Result<WorldDto>> GetByIdAsync(int id)
    {
        var world = await _worldRepo.GetByIdAsync(id);
        if (world is null)
            return Result<WorldDto>.Failure($"Мир с Id {id} не найден");

        return Result<WorldDto>.Success(new WorldDto(world.Id, world.Name, world.Description, world.CurrentDay));
    }

    public async Task<Result<IReadOnlyList<WorldDto>>> GetAllAsync()
    {
        var worlds = await _worldRepo.GetAllAsync();
        return Result<IReadOnlyList<WorldDto>>.Success(
            worlds
                .Select(world => new WorldDto(world.Id, world.Name, world.Description, world.CurrentDay))
                .ToList()
                .AsReadOnly());
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var world = await _worldRepo.GetByIdAsync(id);
        if (world is null)
            return Result.Failure($"Мир с Id {id} не найден");

        await _worldRepo.DeleteAsync(id);
        return Result.Success();
    }
}
