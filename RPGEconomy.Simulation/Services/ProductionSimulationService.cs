using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Simulation.Engine;

namespace RPGEconomy.Simulation.Services;

public class ProductionSimulationService
{
    public void RunTick(SimulationContext ctx)
    {
        ctx.ResetProductionDemand();

        foreach (var settlement in ctx.Settlements)
        {
            if (!ctx.Warehouses.TryGetValue(settlement.Id, out var warehouse)) continue;
            if (!ctx.Buildings.TryGetValue(settlement.Id, out var buildings)) continue;

            foreach (var building in buildings.Where(b => b.IsActive).OrderBy(b => b.Id))
            {
                if (!ctx.Recipes.TryGetValue(building.RecipeId, out var recipe)) continue;

                var plannedBatches = building.BatchesPerDay(recipe.LaborDaysRequired);
                if (plannedBatches <= 0) continue;

                var plannedBatchCount = (decimal)plannedBatches;
                var actualBatchCount = CalculateActualBatchCount(warehouse, recipe, plannedBatchCount);

                AddProductionDemand(ctx, settlement.Id, warehouse, recipe, plannedBatchCount);

                if (actualBatchCount <= 0m) continue;

                foreach (var input in recipe.Inputs)
                {
                    var withdrawal = warehouse.Withdraw(
                        input.ProductTypeId,
                        input.Quantity * actualBatchCount,
                        QualityGrade.Normal);

                    if (!withdrawal.IsSuccess)
                        throw new InvalidOperationException(withdrawal.Error);
                }

                foreach (var output in recipe.Outputs)
                {
                    var addResult = warehouse.AddItem(
                        output.ProductTypeId,
                        output.Quantity * actualBatchCount,
                        QualityGrade.Normal);

                    if (!addResult.IsSuccess)
                        throw new InvalidOperationException(addResult.Error);
                }
            }
        }
    }

    private static decimal CalculateActualBatchCount(
        Warehouse warehouse,
        ProductionRecipe recipe,
        decimal plannedBatchCount)
    {
        if (recipe.Inputs.Count == 0)
            return plannedBatchCount;

        var actualBatchCount = plannedBatchCount;
        foreach (var input in recipe.Inputs)
        {
            var availableQuantity = warehouse.GetAvailableQuantity(input.ProductTypeId, QualityGrade.Normal);
            var inputLimitedBatchCount = availableQuantity / input.Quantity;
            actualBatchCount = decimal.Min(actualBatchCount, inputLimitedBatchCount);
        }

        return decimal.Max(actualBatchCount, 0m);
    }

    private static void AddProductionDemand(
        SimulationContext ctx,
        int settlementId,
        Warehouse warehouse,
        ProductionRecipe recipe,
        decimal plannedBatchCount)
    {
        if (recipe.Inputs.Count == 0)
            return;

        foreach (var input in recipe.Inputs)
        {
            var requiredQuantity = input.Quantity * plannedBatchCount;
            var availableQuantity = warehouse.GetAvailableQuantity(input.ProductTypeId, QualityGrade.Normal);
            var missingQuantity = decimal.Max(requiredQuantity - availableQuantity, 0m);

            ctx.AddProductionDemand(settlementId, input.ProductTypeId, missingQuantity);
        }
    }
}
