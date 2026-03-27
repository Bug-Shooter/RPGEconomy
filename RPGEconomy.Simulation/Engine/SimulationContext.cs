using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.World;

namespace RPGEconomy.Simulation.Engine;

public class SimulationContext
{
    private readonly Dictionary<int, Dictionary<int, decimal>> _productionDemandBySettlement = [];

    public int WorldId { get; }
    public int CurrentDay { get; }

    public IReadOnlyList<Settlement> Settlements { get; }
    public IReadOnlyDictionary<int, Warehouse> Warehouses { get; }
    public IReadOnlyDictionary<int, Market> Markets { get; }
    public IReadOnlyDictionary<int, IReadOnlyList<PopulationGroup>> PopulationGroups { get; }
    public IReadOnlyDictionary<int, IReadOnlyList<Building>> Buildings { get; }
    public IReadOnlyDictionary<int, ProductionRecipe> Recipes { get; }

    public SimulationContext(
        int worldId,
        int currentDay,
        IReadOnlyList<Settlement> settlements,
        IReadOnlyDictionary<int, Warehouse> warehouses,
        IReadOnlyDictionary<int, Market> markets,
        IReadOnlyDictionary<int, IReadOnlyList<PopulationGroup>> populationGroups,
        IReadOnlyDictionary<int, IReadOnlyList<Building>> buildings,
        IReadOnlyDictionary<int, ProductionRecipe> recipes)
    {
        WorldId = worldId;
        CurrentDay = currentDay;
        Settlements = settlements;
        Warehouses = warehouses;
        Markets = markets;
        PopulationGroups = populationGroups;
        Buildings = buildings;
        Recipes = recipes;
    }

    public void ResetProductionDemand() => _productionDemandBySettlement.Clear();

    public void AddProductionDemand(int settlementId, int productTypeId, decimal quantity)
    {
        if (quantity <= 0m)
            return;

        if (!_productionDemandBySettlement.TryGetValue(settlementId, out var demandByProduct))
        {
            demandByProduct = [];
            _productionDemandBySettlement[settlementId] = demandByProduct;
        }

        demandByProduct[productTypeId] = demandByProduct.GetValueOrDefault(productTypeId, 0m) + quantity;
    }

    public IReadOnlyDictionary<int, decimal> GetProductionDemand(int settlementId) =>
        _productionDemandBySettlement.GetValueOrDefault(settlementId) ?? EmptyDemand.Instance;

    private sealed class EmptyDemand : Dictionary<int, decimal>
    {
        public static readonly EmptyDemand Instance = new();
    }
}
