using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Simulation.Engine;

namespace RPGEconomy.Simulation.Services;

public class ProductionSimulationService
{
    // Обрабатывает производство для всех поселений за один тик
    public void RunTick(SimulationContext ctx)
    {
        foreach (var settlement in ctx.Settlements)
        {
            if (!ctx.Warehouses.TryGetValue(settlement.Id, out var warehouse)) continue;
            if (!ctx.Buildings.TryGetValue(settlement.Id, out var buildings)) continue;

            foreach (var building in buildings.Where(b => b.IsActive))
            {
                if (!ctx.Recipes.TryGetValue(building.RecipeId, out var recipe)) continue;

                // Сколько партий здание может произвести за день
                var batches = building.BatchesPerDay(recipe.LaborDaysRequired);
                if (batches <= 0) continue;

                // Масштабируем ингредиенты под количество партий
                var scaledInputs = recipe.Inputs
                    .Select(i => new RecipeIngredient(i.ProductTypeId, i.Quantity * batches))
                    .ToList();

                // Проверяем что склад может выдать все входные ресурсы
                if (!warehouse.CanFulfill(scaledInputs)) continue;

                // Списываем входные ресурсы
                foreach (var input in scaledInputs)
                    warehouse.Withdraw(input.ProductTypeId, input.Quantity, QualityGrade.Normal);

                // Добавляем выходные товары
                foreach (var output in recipe.Outputs)
                    warehouse.AddItem(
                        output.ProductTypeId,
                        output.Quantity * batches,
                        QualityGrade.Normal);
            }
        }
    }
}
