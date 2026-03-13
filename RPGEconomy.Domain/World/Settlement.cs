using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.World;
public class Settlement : AggregateRoot
{
    public string Name { get; private set; }
    public int WorldId { get; private set; }
    public int Population { get; private set; }

    // Dapper
    public Settlement(int id, int worldId, string name, int population) : base(id)
    {
        Name = name;
        WorldId = worldId;
        Population = population;
    }

    public static Settlement Create(string name, int worldId, int population)
        => new Settlement(0, worldId, name, population);

    public void Update(string name, int population)
    {
        Name = name;
        Population = population;
    }
}
