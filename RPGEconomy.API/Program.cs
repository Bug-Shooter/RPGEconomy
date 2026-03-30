using RPGEconomy.API.Middleware;
using RPGEconomy.Application;
using RPGEconomy.Infrastructure;
using RPGEconomy.Infrastructure.Migrations;
using RPGEconomy.Simulation;

namespace RPGEconomy.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //Configure Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Add services to the container.
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure();
        builder.Services.AddSimulation();
        builder.Services.AddSimulationExecutionDecorators();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        //// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        //builder.Services.AddOpenApi(); //standard

        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.Configuration.RunDatabaseMigrations();

        // Register middleware
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapOpenApi();
        }

        //app.UseHttpsRedirection(); standard
        //app.UseAuthorization(); standard
        app.MapControllers();

        app.Run();
    }
}
