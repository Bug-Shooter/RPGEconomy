using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.World;

public class Settlement : AggregateRoot
{
    public string Name { get; private set; }
    public int WorldId { get; private set; }

    // Dapper
    public Settlement(int id, int worldId, string name) : base(id)
    {
        Name = name;
        WorldId = worldId;
    }

    public static Result<Settlement> Create(string name, int worldId)
    {
        var validation = Validate(name, worldId);
        if (!validation.IsSuccess)
            return Result<Settlement>.Failure(validation.Error!);

        return Result<Settlement>.Success(new Settlement(0, worldId, name));
    }

    public Result Update(string name)
    {
        var validation = Validate(name, WorldId);
        if (!validation.IsSuccess)
            return validation;

        Name = name;
        return Result.Success();
    }

    private static Result Validate(string name, int worldId)
    {
        if (worldId <= 0)
            return Result.Failure("Идентификатор мира должен быть больше нуля");

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Название поселения не может быть пустым");

        return Result.Success();
    }
}
