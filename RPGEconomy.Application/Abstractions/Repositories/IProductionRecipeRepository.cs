using RPGEconomy.Domain.Production;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IProductionRecipeRepository : IRepository<ProductionRecipe>
{
    Task<IReadOnlyList<ProductionRecipe>> GetAllAsync();
}
