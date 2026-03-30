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

    public static Result<World> Create(string name, string description)
    {
        var validation = Validate(name, description);
        if (!validation.IsSuccess)
            return Result<World>.Failure(validation.Error!);

        return Result<World>.Success(new World(0, name, description, 0));
    }

    public Result AdvanceDays(int days)
    {
        if (days <= 0)
            return Result.Failure("Количество дней должно быть больше нуля");

        CurrentDay += days;
        return Result.Success();
    }

    public Result Update(string name, string description)
    {
        var validation = Validate(name, description);
        if (!validation.IsSuccess)
            return validation;

        Name = name;
        Description = description;
        return Result.Success();
    }

    private static Result Validate(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Название мира не может быть пустым");

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("Описание мира не может быть пустым");

        return Result.Success();
    }
}
