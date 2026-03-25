using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
        var settingsPath = TestConnectionStrings.GetSettingsPath();

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(settingsPath, optional: false, reloadOnChange: false);
        });

        if (_configureServices is not null)
        {
            builder.ConfigureServices(_configureServices);
        }
    }
}
