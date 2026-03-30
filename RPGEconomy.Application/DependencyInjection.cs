using Microsoft.Extensions.DependencyInjection;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.Services;

namespace RPGEconomy.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBuildingService, BuildingService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IProductTypeService, ProductTypeService>();
        services.AddScoped<IResourceTypeService, ResourceTypeService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<IWorldService, WorldService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IMarketService, MarketService>();
        services.AddScoped<IPopulationGroupService, PopulationGroupService>();
        services.AddScoped<IEconomicEventService, EconomicEventService>();
        services.AddScoped<ISimulationService, SimulationService>();

        return services;
    }
}
