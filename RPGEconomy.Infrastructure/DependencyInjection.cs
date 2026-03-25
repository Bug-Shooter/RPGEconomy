using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Infrastructure.Decorators;
using RPGEconomy.Infrastructure.Persistence;
using RPGEconomy.Infrastructure.Persistence.Repositories;

namespace RPGEconomy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found");

            return new NpgsqlConnectionFactory(connectionString);
        });

        // Repositories - Scrutor scan
        services.Scan(scan => scan
            .FromAssemblyOf<WorldRepository>()
            .AddClasses(c => c.AssignableTo(typeof(IRepository<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Repository interfaces not covered by IRepository<T>
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IMarketRepository, MarketRepository>();
        services.AddScoped<ISettlementRepository, SettlementRepository>();
        services.AddScoped<ISimulationJobRepository, SimulationJobRepository>();

        return services;
    }

    public static IServiceCollection AddSimulationExecutionDecorators(this IServiceCollection services)
    {
        services.Decorate<ISimulationExecutor, LoggingSimulationDecorator>();
        services.Decorate<ISimulationExecutor, TransactionSimulationDecorator>();

        return services;
    }
}
