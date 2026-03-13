using Microsoft.Extensions.DependencyInjection;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.Services;
using RPGEconomy.Infrastructure.Decorators;
using RPGEconomy.Infrastructure.Persistence;
using RPGEconomy.Infrastructure.Persistence.Repositories;
using RPGEconomy.Simulation.Engine;
using RPGEconomy.Simulation.Services;

namespace RPGEconomy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // DbConnectionFactory
        services.AddSingleton<IDbConnectionFactory>(
            _ => new NpgsqlConnectionFactory(connectionString));

        // Repositories — Scrutor scan
        services.Scan(scan => scan
            .FromAssemblyOf<WorldRepository>()
            .AddClasses(c => c.AssignableTo(typeof(IRepository<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Интерфейсы репозиториев не покрытые IRepository<T>
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IMarketRepository, MarketRepository>();
        services.AddScoped<ISettlementRepository, SettlementRepository>();
        services.AddScoped<IBuildingService, BuildingService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IProductTypeService, ProductTypeService>();
        services.AddScoped<IResourceTypeService, ResourceTypeService>();
        services.AddScoped<ICurrencyService, CurrencyService>();

        // SimulationEngine + декораторы через Scrutor
        services.AddScoped<ISimulationEngine, SimulationEngine>();
        services.Decorate<ISimulationEngine, LoggingSimulationDecorator>();
        services.Decorate<ISimulationEngine, TransactionSimulationDecorator>();

        // Application Services //Todo: Скорее всего разделение не нада и можно вверх кинуть
        services.AddScoped<IWorldService, WorldService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IMarketService, MarketService>();

        // Simulation internal services
        services.AddScoped<ProductionSimulationService>();
        services.AddScoped<MarketSimulationService>();

        return services;
    }
}

