using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Resources;

public class ProductType : AggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public double WeightPerUnit { get; private set; }

    // Dapper
    public ProductType(int id, string name, string description, decimal basePrice, double weightPerUnit) : base(id)
    {
        Name = name;
        Description = description;
        BasePrice = basePrice;
        WeightPerUnit = weightPerUnit;
    }

    public static Result<ProductType> Create(string name, string description, decimal basePrice, double weightPerUnit)
    {
        var validation = Validate(name, description, basePrice, weightPerUnit);
        if (!validation.IsSuccess)
            return Result<ProductType>.Failure(validation.Error!);

        return Result<ProductType>.Success(new ProductType(0, name, description, basePrice, weightPerUnit));
    }

    public Result Update(string name, string description, decimal basePrice, double weightPerUnit)
    {
        var validation = Validate(name, description, basePrice, weightPerUnit);
        if (!validation.IsSuccess)
            return validation;

        Name = name;
        Description = description;
        BasePrice = basePrice;
        WeightPerUnit = weightPerUnit;
        return Result.Success();
    }

    private static Result Validate(string name, string description, decimal basePrice, double weightPerUnit)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Название товара не может быть пустым");

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("Описание товара не может быть пустым");

        if (basePrice <= 0m)
            return Result.Failure("Базовая цена должна быть больше нуля");

        if (weightPerUnit <= 0d)
            return Result.Failure("Вес единицы товара должен быть больше нуля");

        return Result.Success();
    }
}
