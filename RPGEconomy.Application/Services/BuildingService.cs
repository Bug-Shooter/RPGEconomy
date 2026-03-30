using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Production;

namespace RPGEconomy.Application.Services;

public class BuildingService : IBuildingService
{
    private readonly IBuildingRepository _buildingRepo;
    private readonly ISettlementRepository _settlementRepo;
    private readonly IProductionRecipeRepository _recipeRepo;

    public BuildingService(
        IBuildingRepository buildingRepo,
        ISettlementRepository settlementRepo,
        IProductionRecipeRepository recipeRepo)
    {
        _buildingRepo = buildingRepo;
        _settlementRepo = settlementRepo;
        _recipeRepo = recipeRepo;
    }

    public async Task<Result<BuildingDto>> GetByIdAsync(int id)
    {
        var building = await _buildingRepo.GetByIdAsync(id);
        if (building is null)
            return Result<BuildingDto>.Failure($"Здание с Id {id} не найдено");

        return Result<BuildingDto>.Success(ToDto(building));
    }

    public async Task<Result<IReadOnlyList<BuildingDto>>> GetBySettlementIdAsync(int settlementId)
    {
        var buildings = await _buildingRepo.GetBySettlementIdAsync(settlementId);
        return Result<IReadOnlyList<BuildingDto>>.Success(buildings.Select(ToDto).ToList().AsReadOnly());
    }

    public async Task<Result<BuildingDto>> CreateAsync(
        int settlementId,
        string name,
        int recipeId,
        int workerCount,
        decimal inputReserveCoverageTicks)
    {
        var settlement = await _settlementRepo.GetByIdAsync(settlementId);
        if (settlement is null)
            return Result<BuildingDto>.Failure($"Поселение с Id {settlementId} не найдено");

        var recipe = await _recipeRepo.GetByIdAsync(recipeId);
        if (recipe is null)
            return Result<BuildingDto>.Failure($"Рецепт с Id {recipeId} не найден");

        var createResult = Building.Create(name, settlementId, recipeId, workerCount, inputReserveCoverageTicks);
        if (!createResult.IsSuccess)
            return Result<BuildingDto>.Failure(createResult.Error!);

        var building = createResult.Value!;
        var id = await _buildingRepo.SaveAsync(building);
        return Result<BuildingDto>.Success(ToDto(building) with { Id = id });
    }

    public async Task<Result<BuildingDto>> UpdateAsync(int id, string name, int workerCount, decimal inputReserveCoverageTicks)
    {
        var building = await _buildingRepo.GetByIdAsync(id);
        if (building is null)
            return Result<BuildingDto>.Failure($"Здание с Id {id} не найдено");

        var updateResult = building.Update(name, workerCount, inputReserveCoverageTicks);
        if (!updateResult.IsSuccess)
            return Result<BuildingDto>.Failure(updateResult.Error!);

        await _buildingRepo.SaveAsync(building);
        return Result<BuildingDto>.Success(ToDto(building));
    }

    public async Task<Result> ActivateAsync(int id)
    {
        var building = await _buildingRepo.GetByIdAsync(id);
        if (building is null)
            return Result.Failure($"Здание с Id {id} не найдено");

        var result = building.Activate();
        if (!result.IsSuccess)
            return result;

        await _buildingRepo.SaveAsync(building);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        var building = await _buildingRepo.GetByIdAsync(id);
        if (building is null)
            return Result.Failure($"Здание с Id {id} не найдено");

        var result = building.Deactivate();
        if (!result.IsSuccess)
            return result;

        await _buildingRepo.SaveAsync(building);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var building = await _buildingRepo.GetByIdAsync(id);
        if (building is null)
            return Result.Failure($"Здание с Id {id} не найдено");

        await _buildingRepo.DeleteAsync(id);
        return Result.Success();
    }

    private static BuildingDto ToDto(Building building) =>
        new(
            building.Id,
            building.Name,
            building.SettlementId,
            building.RecipeId,
            building.WorkerCount,
            building.IsActive,
            building.InputReserveCoverageTicks,
            building.InputReserveItems
                .Select(item => new ReserveStockItemDto(item.ProductTypeId, item.Quantity))
                .ToList()
                .AsReadOnly());
}
