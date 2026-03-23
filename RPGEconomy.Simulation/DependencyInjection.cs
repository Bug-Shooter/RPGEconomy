using Microsoft.Extensions.DependencyInjection;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Simulation.Engine;
using RPGEconomy.Simulation.Services;

namespace RPGEconomy.Simulation;

public static class DependencyInjection
{
    public static IServiceCollection AddSimulation(this IServiceCollection services)
    {
        services.AddScoped<ISimulationExecutor, SimulationEngine>();
        services.AddScoped<ProductionSimulationService>();
        services.AddScoped<MarketSimulationService>();

        return services;
    }
}
