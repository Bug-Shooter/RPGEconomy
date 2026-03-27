using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.World;

namespace RPGEconomy.Simulation.Engine;

public class SimulationContext
{
    public int WorldId { get; }
    public int CurrentDay { get; }

    public IReadOnlyList<Settlement> Settlements { get; }
    public IReadOnlyDictionary<int, Warehouse> Warehouses { get; }    // key: settlementId
    public IReadOnlyDictionary<int, Market> Markets { get; }          // key: settlementId
    public IReadOnlyDictionary<int, IReadOnlyList<PopulationGroup>> PopulationGroups { get; } // key: settlementId
    public IReadOnlyDictionary<int, IReadOnlyList<Building>> Buildings { get; } // key: settlementId
    public IReadOnlyDictionary<int, ProductionRecipe> Recipes { get; } // key: recipeId

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
}
