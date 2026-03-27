using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Population;

namespace RPGEconomy.Application.Services;

public class PopulationGroupService : IPopulationGroupService
{
    private readonly IPopulationGroupRepository _populationGroupRepo;
    private readonly ISettlementRepository _settlementRepo;
    private readonly IProductTypeRepository _productTypeRepo;

    public PopulationGroupService(
        IPopulationGroupRepository populationGroupRepo,
        ISettlementRepository settlementRepo,
        IProductTypeRepository productTypeRepo)
    {
        _populationGroupRepo = populationGroupRepo;
        _settlementRepo = settlementRepo;
        _productTypeRepo = productTypeRepo;
    }

    public async Task<Result<IReadOnlyList<PopulationGroupDto>>> GetBySettlementIdAsync(int settlementId)
    {
        var settlement = await _settlementRepo.GetByIdAsync(settlementId);
        if (settlement is null)
            return Result<IReadOnlyList<PopulationGroupDto>>.Failure($"Settlement with Id {settlementId} was not found");

        var groups = await _populationGroupRepo.GetBySettlementIdAsync(settlementId);
        return Result<IReadOnlyList<PopulationGroupDto>>.Success(groups.Select(ToDto).ToList().AsReadOnly());
    }

    public async Task<Result<PopulationGroupDto>> GetByIdAsync(int id)
    {
        var group = await _populationGroupRepo.GetByIdAsync(id);
        if (group is null)
            return Result<PopulationGroupDto>.Failure($"Population group with Id {id} was not found");

        return Result<PopulationGroupDto>.Success(ToDto(group));
    }

    public async Task<Result<PopulationGroupDto>> CreateAsync(
        int settlementId,
        string name,
        int populationSize,
        IReadOnlyList<ConsumptionProfileItemDto> consumptionProfile)
    {
        var settlement = await _settlementRepo.GetByIdAsync(settlementId);
        if (settlement is null)
            return Result<PopulationGroupDto>.Failure($"Settlement with Id {settlementId} was not found");

        var validation = await ValidateProductTypesAsync(consumptionProfile);
        if (!validation.IsSuccess)
            return Result<PopulationGroupDto>.Failure(validation.Error!);

        var createResult = PopulationGroup.Create(
            settlementId,
            name,
            populationSize,
            consumptionProfile.Select(item => (item.ProductTypeId, item.AmountPerPersonPerTick)));

        if (!createResult.IsSuccess)
            return Result<PopulationGroupDto>.Failure(createResult.Error!);

        var id = await _populationGroupRepo.SaveAsync(createResult.Value!);
        await SyncSettlementPopulationAsync(settlementId);

        var saved = await _populationGroupRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Created population group was not found");

        return Result<PopulationGroupDto>.Success(ToDto(saved));
    }

    public async Task<Result<PopulationGroupDto>> UpdateAsync(
        int id,
        string name,
        int populationSize,
        IReadOnlyList<ConsumptionProfileItemDto> consumptionProfile)
    {
        var group = await _populationGroupRepo.GetByIdAsync(id);
        if (group is null)
            return Result<PopulationGroupDto>.Failure($"Population group with Id {id} was not found");

        var validation = await ValidateProductTypesAsync(consumptionProfile);
        if (!validation.IsSuccess)
            return Result<PopulationGroupDto>.Failure(validation.Error!);

        var updateResult = group.Update(
            name,
            populationSize,
            consumptionProfile.Select(item => (item.ProductTypeId, item.AmountPerPersonPerTick)));

        if (!updateResult.IsSuccess)
            return Result<PopulationGroupDto>.Failure(updateResult.Error!);

        await _populationGroupRepo.SaveAsync(group);
        await SyncSettlementPopulationAsync(group.SettlementId);

        return Result<PopulationGroupDto>.Success(ToDto(group));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var group = await _populationGroupRepo.GetByIdAsync(id);
        if (group is null)
            return Result.Failure($"Population group with Id {id} was not found");

        await _populationGroupRepo.DeleteAsync(id);
        await SyncSettlementPopulationAsync(group.SettlementId);
        return Result.Success();
    }

    private async Task<Result> ValidateProductTypesAsync(IEnumerable<ConsumptionProfileItemDto> consumptionProfile)
    {
        foreach (var item in consumptionProfile)
        {
            var productType = await _productTypeRepo.GetByIdAsync(item.ProductTypeId);
            if (productType is null)
                return Result.Failure($"Product type with Id {item.ProductTypeId} was not found");
        }

        return Result.Success();
    }

    private async Task SyncSettlementPopulationAsync(int settlementId)
    {
        var settlement = await _settlementRepo.GetByIdAsync(settlementId)
            ?? throw new InvalidOperationException("Settlement for population sync was not found");

        var groups = await _populationGroupRepo.GetBySettlementIdAsync(settlementId);
        settlement.Update(settlement.Name, groups.Sum(group => group.PopulationSize));
        await _settlementRepo.SaveAsync(settlement);
    }

    private static PopulationGroupDto ToDto(PopulationGroup group) =>
        new(
            group.Id,
            group.SettlementId,
            group.Name,
            group.PopulationSize,
            group.ConsumptionProfile
                .Select(item => new ConsumptionProfileItemDto(item.ProductTypeId, item.AmountPerPersonPerTick))
                .ToList()
                .AsReadOnly());
}
