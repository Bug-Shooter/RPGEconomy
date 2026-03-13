using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.World;
public class World : AggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int CurrentDay { get; private set; }

    // Dapper
    public World(int id, string name, string description, int currentDay) : base(id)
    {
        Name = name;
        Description = description;
        CurrentDay = currentDay;
    }

    public static World Create(string name, string description)
        => new(0, name, description, 0);

    public Result AdvanceDays(int days)
    {
        if (days <= 0) return Result.Failure("Количество дней должно быть больше нуля");
        CurrentDay += days;
        return Result.Success();
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
