using RPGEconomy.API.Middleware;
using RPGEconomy.Infrastructure;
using RPGEconomy.Infrastructure.Migrations;

namespace RPGEconomy.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("Connection string not found");

        // Add services to the container.
        builder.Services.AddInfrastructure(connectionString);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        //// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        //builder.Services.AddOpenApi(); //стандарт

        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        //Мигрируем Бд при старте
        var migrationRunner = new MigrationRunner(connectionString);
        migrationRunner.Run();

        //Регистрируем Middleware
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapOpenApi();
        }

        //app.UseHttpsRedirection(); стандарт
        //app.UseAuthorization(); стандарт
        app.MapControllers();

        app.Run();
    }
}
