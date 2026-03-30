using RPGEconomy.Domain.Events;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.World;

namespace RPGEconomy.Simulation.Engine;

public class SimulationContext
{
    private readonly Dictionary<int, Dictionary<int, decimal>> _productionDemandBySettlement = [];
    private readonly Dictionary<int, List<DemandRequest>> _consumptionDemandBySettlement = [];

    public int WorldId { get; }
    public int CurrentDay { get; private set; }

    public IReadOnlyList<Settlement> Settlements { get; }
    public IReadOnlyDictionary<int, Warehouse> Warehouses { get; }
    public IReadOnlyDictionary<int, Market> Markets { get; }
    public IReadOnlyDictionary<int, IReadOnlyList<PopulationGroup>> PopulationGroups { get; }
    public IReadOnlyDictionary<int, IReadOnlyList<Building>> Buildings { get; }
    public IReadOnlyDictionary<int, ProductionRecipe> Recipes { get; }
    public IReadOnlyDictionary<int, IReadOnlyList<EconomicEvent>> EconomicEvents { get; }

    public SimulationContext(
        int worldId,
        int currentDay,
        IReadOnlyList<Settlement> settlements,
        IReadOnlyDictionary<int, Warehouse> warehouses,
        IReadOnlyDictionary<int, Market> markets,
        IReadOnlyDictionary<int, IReadOnlyList<PopulationGroup>> populationGroups,
        IReadOnlyDictionary<int, IReadOnlyList<Building>> buildings,
        IReadOnlyDictionary<int, ProductionRecipe> recipes,
        IReadOnlyDictionary<int, IReadOnlyList<EconomicEvent>> economicEvents)
    {
        WorldId = worldId;
        CurrentDay = currentDay;
        Settlements = settlements;
        Warehouses = warehouses;
        Markets = markets;
        PopulationGroups = populationGroups;
        Buildings = buildings;
        Recipes = recipes;
        EconomicEvents = economicEvents;
    }

    public void ResetProductionDemand() => _productionDemandBySettlement.Clear();
    public void ResetConsumptionDemand() => _consumptionDemandBySettlement.Clear();

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

    public void AddConsumptionDemandRequest(int settlementId, int ownerId, int productTypeId, decimal quantity)
    {
        if (quantity <= 0m)
            return;

        if (!_consumptionDemandBySettlement.TryGetValue(settlementId, out var requests))
        {
            requests = [];
            _consumptionDemandBySettlement[settlementId] = requests;
        }

        requests.Add(new DemandRequest(ownerId, productTypeId, quantity));
    }

    public IReadOnlyDictionary<int, decimal> GetProductionDemand(int settlementId) =>
        _productionDemandBySettlement.GetValueOrDefault(settlementId) ?? EmptyDemand.Instance;

    public IReadOnlyList<DemandRequest> GetConsumptionDemandRequests(int settlementId) =>
        _consumptionDemandBySettlement.GetValueOrDefault(settlementId) ?? [];

    public IReadOnlyDictionary<int, decimal> GetConsumptionDemand(int settlementId)
    {
        var requests = GetConsumptionDemandRequests(settlementId);
        if (requests.Count == 0)
            return EmptyDemand.Instance;

        return requests
            .GroupBy(request => request.ProductTypeId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
    }

    public IReadOnlyList<EconomicEffect> GetActiveEffects(int settlementId)
    {
        if (!EconomicEvents.TryGetValue(settlementId, out var economicEvents))
            return [];

        return economicEvents
            .SelectMany(item => item.GetActiveEffects(CurrentDay))
            .ToList()
            .AsReadOnly();
    }

    public void AdvanceDay() => CurrentDay++;

    private sealed class EmptyDemand : Dictionary<int, decimal>
    {
        public static readonly EmptyDemand Instance = new();
    }
}
