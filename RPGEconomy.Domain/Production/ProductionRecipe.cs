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

    // Dapper - ingredients are loaded separately through LoadIngredients.
    public ProductionRecipe(int id, string name, double laborDaysRequired) : base(id)
    {
        Name = name;
        LaborDaysRequired = laborDaysRequired;
    }

    public static Result<ProductionRecipe> Create(
        string name,
        double laborDaysRequired,
        IEnumerable<RecipeIngredient> inputs,
        IEnumerable<RecipeIngredient> outputs)
    {
        var inputList = inputs.ToList();
        var outputList = outputs.ToList();

        var validation = Validate(name, laborDaysRequired, inputList, outputList);
        if (!validation.IsSuccess)
            return Result<ProductionRecipe>.Failure(validation.Error!);

        var recipe = new ProductionRecipe(0, name, laborDaysRequired);
        recipe.LoadIngredients(inputList, outputList);
        return Result<ProductionRecipe>.Success(recipe);
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

    public Result Update(
        string name,
        double laborDaysRequired,
        IEnumerable<RecipeIngredient> inputs,
        IEnumerable<RecipeIngredient> outputs)
    {
        var inputList = inputs.ToList();
        var outputList = outputs.ToList();

        var validation = Validate(name, laborDaysRequired, inputList, outputList);
        if (!validation.IsSuccess)
            return validation;

        Name = name;
        LaborDaysRequired = laborDaysRequired;
        LoadIngredients(inputList, outputList);
        return Result.Success();
    }

    private static Result Validate(
        string name,
        double laborDaysRequired,
        IReadOnlyList<RecipeIngredient> inputs,
        IReadOnlyList<RecipeIngredient> outputs)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Название рецепта не может быть пустым");

        if (laborDaysRequired <= 0)
            return Result.Failure("Трудозатраты должны быть больше нуля");

        if (outputs.Count == 0)
            return Result.Failure("Рецепт должен содержать хотя бы один выходной товар");

        if (inputs.Any(i => i.ProductTypeId <= 0) || outputs.Any(o => o.ProductTypeId <= 0))
            return Result.Failure("Идентификатор товара должен быть больше нуля");

        if (inputs.Any(i => i.Quantity <= 0m))
            return Result.Failure("Количество входного ресурса должно быть больше нуля");

        if (outputs.Any(o => o.Quantity <= 0m))
            return Result.Failure("Количество выходного товара должно быть больше нуля");

        if (inputs.GroupBy(i => i.ProductTypeId).Any(group => group.Count() > 1))
            return Result.Failure("Рецепт не может содержать дублирующиеся входные ресурсы");

        if (outputs.GroupBy(o => o.ProductTypeId).Any(group => group.Count() > 1))
            return Result.Failure("Рецепт не может содержать дублирующиеся выходные товары");

        return Result.Success();
    }
}
