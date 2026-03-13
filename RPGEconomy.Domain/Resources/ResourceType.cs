using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Resources;

public class ResourceType : AggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsRenewable { get; private set; }
    public double RegenerationRatePerDay { get; private set; }

    // Dapper
    public ResourceType(int id, string name, string description,
        bool isRenewable, double regenerationRatePerDay) : base(id)
    {
        Name = name;
        Description = description;
        IsRenewable = isRenewable;
        RegenerationRatePerDay = regenerationRatePerDay;
    }

    public static ResourceType Create(string name, string description,
        bool isRenewable, double regenerationRatePerDay)
        => new(0, name, description, isRenewable, regenerationRatePerDay);

    public void Update(string name, string description, bool isRenewable, double regenerationRatePerDay)
    {
        Name = name;
        Description = description;
        IsRenewable = isRenewable;
        RegenerationRatePerDay = regenerationRatePerDay;
    }
}
