using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Resources;

public class ProductType : AggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public double BasePrice { get; private set; }
    public double WeightPerUnit { get; private set; }

    // Dapper
    public ProductType(int id, string name, string description,
        double basePrice, double weightPerUnit) : base(id)
    {
        Name = name;
        Description = description;
        BasePrice = basePrice;
        WeightPerUnit = weightPerUnit;
    }

    public static ProductType Create(string name, string description,
        double basePrice, double weightPerUnit)
        => new(0, name, description, basePrice, weightPerUnit);

    public void Update(string name, string description, double basePrice, double weightPerUnit)
    {
        Name = name;
        Description = description;
        BasePrice = basePrice;
        WeightPerUnit = weightPerUnit;
    }
}
