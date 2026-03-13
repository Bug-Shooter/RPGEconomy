namespace RPGEconomy.Domain.Resources;
public class QualityGrade
{
    public static readonly QualityGrade Low = new("Low", 0.6f);
    public static readonly QualityGrade Normal = new("Normal", 1.0f);
    public static readonly QualityGrade High = new("High", 1.5f);

    public string Name { get; }
    public double PriceMultiplier { get; }

    private QualityGrade(string name, double multiplier)
    {
        Name = name;
        PriceMultiplier = multiplier;
    }

    public static QualityGrade FromName(string name) => name switch
    {
        "Low" => Low,
        "High" => High,
        _ => Normal
    };
}
