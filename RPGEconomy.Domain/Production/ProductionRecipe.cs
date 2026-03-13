using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Production;

public class ProductionRecipe : AggregateRoot
{
    private readonly List<RecipeIngredient> _inputs = new();
    private readonly List<RecipeIngredient> _outputs = new();

    public string Name { get; private set; }
    public double LaborDaysRequired { get; private set; }
    public IReadOnlyList<RecipeIngredient> Inputs => _inputs.AsReadOnly();
    public IReadOnlyList<RecipeIngredient> Outputs => _outputs.AsReadOnly();

    // Dapper — ингредиенты загружаются отдельно через LoadIngredients
    public ProductionRecipe(int id, string name, double laborDaysRequired) : base(id)
    {
        Name = name;
        LaborDaysRequired = laborDaysRequired;
    }

    public static ProductionRecipe Create(string name, double laborDaysRequired,
        IEnumerable<RecipeIngredient> inputs, IEnumerable<RecipeIngredient> outputs)
    {
        var recipe = new ProductionRecipe(0, name, laborDaysRequired);
        recipe._inputs.AddRange(inputs);
        recipe._outputs.AddRange(outputs);
        return recipe;
    }

    internal void LoadIngredients(
        IEnumerable<RecipeIngredient> inputs,
        IEnumerable<RecipeIngredient> outputs)
    {
        _inputs.Clear();
        _inputs.AddRange(inputs);
        _outputs.Clear();
        _outputs.AddRange(outputs);
    }

    public void Update(string name, double laborDaysRequired, IEnumerable<RecipeIngredient> inputs, IEnumerable<RecipeIngredient> outputs)
    {
        Name = name;
        LaborDaysRequired = laborDaysRequired;
        LoadIngredients(inputs, outputs);
    }
}
