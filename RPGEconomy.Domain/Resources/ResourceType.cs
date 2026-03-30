using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Resources;

public class ResourceType : AggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsRenewable { get; private set; }
    public double RegenerationRatePerDay { get; private set; }

    // Dapper
    public ResourceType(int id, string name, string description, bool isRenewable, double regenerationRatePerDay) : base(id)
    {
        Name = name;
        Description = description;
        IsRenewable = isRenewable;
        RegenerationRatePerDay = regenerationRatePerDay;
    }

    public static Result<ResourceType> Create(string name, string description, bool isRenewable, double regenerationRatePerDay)
    {
        var validation = Validate(name, description, regenerationRatePerDay);
        if (!validation.IsSuccess)
            return Result<ResourceType>.Failure(validation.Error!);

        return Result<ResourceType>.Success(
            new ResourceType(0, name, description, isRenewable, regenerationRatePerDay));
    }

    public Result Update(string name, string description, bool isRenewable, double regenerationRatePerDay)
    {
        var validation = Validate(name, description, regenerationRatePerDay);
        if (!validation.IsSuccess)
            return validation;

        Name = name;
        Description = description;
        IsRenewable = isRenewable;
        RegenerationRatePerDay = regenerationRatePerDay;
        return Result.Success();
    }

    private static Result Validate(string name, string description, double regenerationRatePerDay)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Название ресурса не может быть пустым");

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("Описание ресурса не может быть пустым");

        if (regenerationRatePerDay < 0d)
            return Result.Failure("Скорость восстановления не может быть отрицательной");

        return Result.Success();
    }
}
