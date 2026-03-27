using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Production;

public class Building : AggregateRoot
{
    private readonly List<BuildingInputReserveItem> _inputReserveItems = [];

    public string Name { get; private set; }
    public int SettlementId { get; private set; }
    public int RecipeId { get; private set; }
    public int WorkerCount { get; private set; }
    public bool IsActive { get; private set; }
    public decimal InputReserveCoverageTicks { get; private set; }
    public IReadOnlyList<BuildingInputReserveItem> InputReserveItems => _inputReserveItems.AsReadOnly();

    // Dapper
    public Building(int id, string name, int settlementId,
        int recipeId, int workerCount, bool isActive, decimal inputReserveCoverageTicks = 0m) : base(id)
    {
        Name = name;
        SettlementId = settlementId;
        RecipeId = recipeId;
        WorkerCount = workerCount;
        IsActive = isActive;
        InputReserveCoverageTicks = inputReserveCoverageTicks;
    }

    public static Building Create(string name, int settlementId,
        int recipeId, int workerCount, decimal inputReserveCoverageTicks = 0m)
        => new(0, name, settlementId, recipeId, workerCount, true, inputReserveCoverageTicks);

    public void Update(string name, int workerCount, decimal inputReserveCoverageTicks)
    {
        Name = name;
        WorkerCount = workerCount;
        InputReserveCoverageTicks = inputReserveCoverageTicks;
    }

    public void Update(string name, int workerCount) =>
        Update(name, workerCount, InputReserveCoverageTicks);

    public int BatchesPerDay(double laborDaysPerBatch) =>
        laborDaysPerBatch <= 0
            ? 0
            : (int)Math.Floor(WorkerCount / laborDaysPerBatch);

    public IReadOnlyDictionary<int, decimal> CalculatePlannedInputDemand(ProductionRecipe recipe)
    {
        var plannedBatchCount = BatchesPerDay(recipe.LaborDaysRequired);
        if (plannedBatchCount <= 0)
            return new Dictionary<int, decimal>();

        return recipe.Inputs
            .Where(input => input.Quantity > 0m)
            .GroupBy(input => input.ProductTypeId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(input => input.Quantity) * plannedBatchCount);
    }

    public IReadOnlyDictionary<int, decimal> CalculateDesiredInputReserve(
        ProductionRecipe recipe,
        IReadOnlyDictionary<int, decimal>? coverageMultipliersByProduct = null)
    {
        var plannedDemand = CalculatePlannedInputDemand(recipe);
        return plannedDemand.ToDictionary(
            x => x.Key,
            x =>
            {
                var multiplier = coverageMultipliersByProduct?.GetValueOrDefault(x.Key, 1m) ?? 1m;
                return x.Value * InputReserveCoverageTicks * multiplier;
            });
    }

    public IReadOnlyDictionary<int, decimal> CalculateReserveDemand(
        ProductionRecipe recipe,
        IReadOnlyDictionary<int, decimal>? coverageMultipliersByProduct = null)
    {
        var desiredReserve = CalculateDesiredInputReserve(recipe, coverageMultipliersByProduct);
        return desiredReserve
            .Select(x => new KeyValuePair<int, decimal>(x.Key, decimal.Max(x.Value - GetInputReserveQuantity(x.Key), 0m)))
            .Where(x => x.Value > 0m)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public decimal GetInputReserveQuantity(int productTypeId) =>
        _inputReserveItems
            .Where(item => item.ProductTypeId == productTypeId)
            .Sum(item => item.Quantity);

    public Result ReceiveInputReserve(int productTypeId, decimal quantity)
    {
        if (quantity <= 0m)
            return Result.Failure("Input reserve quantity must be greater than zero");

        var existing = _inputReserveItems.FirstOrDefault(item => item.ProductTypeId == productTypeId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
            return Result.Success();
        }

        var createResult = BuildingInputReserveItem.Create(Id, productTypeId, quantity);
        if (!createResult.IsSuccess)
            return Result.Failure(createResult.Error!);

        _inputReserveItems.Add(createResult.Value!);
        return Result.Success();
    }

    public Result ConsumeInputReserve(int productTypeId, decimal quantity)
    {
        if (quantity <= 0m)
            return Result.Failure("Input reserve quantity must be greater than zero");

        var item = _inputReserveItems.FirstOrDefault(x => x.ProductTypeId == productTypeId);
        if (item is null || item.Quantity < quantity)
            return Result.Failure("Not enough input reserve stock");

        item.DecreaseQuantity(quantity);
        if (item.Quantity == 0m)
            _inputReserveItems.Remove(item);

        return Result.Success();
    }

    internal void LoadInputReserveItems(IEnumerable<BuildingInputReserveItem> inputReserveItems)
    {
        _inputReserveItems.Clear();
        _inputReserveItems.AddRange(inputReserveItems);
    }

    public Result Deactivate()
    {
        if (!IsActive) return Result.Failure("Здание уже неактивно");
        IsActive = false;
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive) return Result.Failure("Здание уже активно");
        IsActive = true;
        return Result.Success();
    }

}

