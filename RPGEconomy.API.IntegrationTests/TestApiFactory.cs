using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPGEconomy.API;
using RPGEconomy.Testing;

namespace RPGEconomy.API.IntegrationTests;

public class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureServices;

    public TestApiFactory(Action<IServiceCollection>? configureServices = null)
        => _configureServices = configureServices;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            TestConnectionStrings.TestDatabase);
        builder.UseEnvironment("Development");
        
        // Не работает. Пропускает часть запросов в основную БД.
        //builder.ConfigureAppConfiguration((_, config) =>
        //{
        //    config.AddInMemoryCollection(new Dictionary<string, string?>
        //    {
        //        ["ConnectionStrings:DefaultConnection"] = TestConnectionStrings.TestDatabase
        //    });
        //});

        if (_configureServices is not null)
        {
            builder.ConfigureServices(_configureServices);
        }
    }
}
