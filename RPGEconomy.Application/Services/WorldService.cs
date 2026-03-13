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
        if (string.IsNullOrWhiteSpace(name))
            return Result<WorldDto>.Failure("Название мира не может быть пустым");

        var world = World.Create(name, description);
        var id = await _worldRepo.SaveAsync(world);

        return Result<WorldDto>.Success(new WorldDto(id, world.Name, world.Description, 0));
    }

    public async Task<Result<WorldDto>> UpdateAsync(int id, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<WorldDto>.Failure("Название мира не может быть пустым");

        var world = await _worldRepo.GetByIdAsync(id);
        if (world is null)
            return Result<WorldDto>.Failure($"Мир с Id {id} не найден");

        world.Update(name, description);
        await _worldRepo.SaveAsync(world);

        return Result<WorldDto>.Success(
            new WorldDto(world.Id, world.Name, world.Description, world.CurrentDay));
    }

    public async Task<Result<WorldDto>> GetByIdAsync(int id)
    {
        var world = await _worldRepo.GetByIdAsync(id);
        if (world is null)
            return Result<WorldDto>.Failure($"Мир с Id {id} не найден");

        return Result<WorldDto>.Success(
            new WorldDto(world.Id, world.Name, world.Description, world.CurrentDay));
    }

    public async Task<Result<IReadOnlyList<WorldDto>>> GetAllAsync()
    {
        var worlds = await _worldRepo.GetAllAsync();

        var dtos = worlds
            .Select(w => new WorldDto(w.Id, w.Name, w.Description, w.CurrentDay))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<WorldDto>>.Success(dtos);
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
