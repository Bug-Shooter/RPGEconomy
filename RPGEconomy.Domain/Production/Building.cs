using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Production;

public class Building : AggregateRoot
{
    public string Name { get; private set; }
    public int SettlementId { get; private set; }
    public int RecipeId { get; private set; }
    public int WorkerCount { get; private set; }
    public bool IsActive { get; private set; }

    // Dapper
    public Building(int id, string name, int settlementId,
        int recipeId, int workerCount, bool isActive) : base(id)
    {
        Name = name;
        SettlementId = settlementId;
        RecipeId = recipeId;
        WorkerCount = workerCount;
        IsActive = isActive;
    }

    public static Building Create(string name, int settlementId,
        int recipeId, int workerCount)
        => new(0, name, settlementId, recipeId, workerCount, true);
    public void Update(string name, int workerCount)
    {
        Name = name;
        WorkerCount = workerCount;
    }

    public int BatchesPerDay(double laborDaysPerBatch) =>
        laborDaysPerBatch <= 0
            ? 0
            : (int)Math.Floor(WorkerCount / laborDaysPerBatch);

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

