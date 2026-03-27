using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Testing;
using System.Net;
using System.Net.Http.Json;

namespace RPGEconomy.API.IntegrationTests;

[Collection(IntegrationTestCollection.Database)]
public class SimulationAndMiddlewareApiTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly TestApiFactory _factory = new();

    public SimulationAndMiddlewareApiTests(DatabaseFixture fixture)
        => _fixture = fixture;

    public ValueTask InitializeAsync() =>
        new(PostgresTestDatabase.ResetAsync());

    public ValueTask DisposeAsync() =>
        new(Task.CompletedTask);

    [Fact]
    public async Task AdvanceSimulation_Should_Return_Ok_And_Advance_Days()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync(currentDay: 5);

        var response = await client.PostAsJsonAsync($"/api/worlds/{worldId}/simulation/advance", new { days = 2 }, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<SimulationResultResponse>(cancellationToken: TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();
        payload!.DaysBefore.Should().Be(5);
        payload.DaysAfter.Should().Be(7);
    }

    [Fact]
    public async Task AdvanceSimulation_Should_Update_Market_From_Buildings_And_Population_Groups()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync(currentDay: 1);
        var productId = await seed.CreateProductTypeAsync("Bread", "Desc", 10m, 1);
        var recipeId = await seed.CreateRecipeAsync("Bakery", 1, [], [(productId, 1m)]);

        var settlementResponse = await client.PostAsJsonAsync(
            $"/api/worlds/{worldId}/settlements",
            new { name = "Town", population = 0 },
            cancellationToken: TestContext.Current.CancellationToken);
        settlementResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var settlement = await settlementResponse.Content.ReadFromJsonAsync<SettlementResponse>(cancellationToken: TestContext.Current.CancellationToken);
        settlement.Should().NotBeNull();

        var marketRegistration = await client.PostAsJsonAsync(
            $"/api/settlements/{settlement!.SettlementId}/market/products",
            new { productTypeId = productId, initialPrice = 10m },
            cancellationToken: TestContext.Current.CancellationToken);
        marketRegistration.StatusCode.Should().Be(HttpStatusCode.OK);

        var buildingResponse = await client.PostAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/buildings",
            new { name = "Bakery", recipeId, workerCount = 2 },
            cancellationToken: TestContext.Current.CancellationToken);
        buildingResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var populationGroupResponse = await client.PostAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/population-groups",
            new
            {
                name = "Peasants",
                populationSize = 50,
                consumptionProfile = new[] { new { productTypeId = productId, amountPerPersonPerTick = 0.1m } }
            },
            cancellationToken: TestContext.Current.CancellationToken);
        populationGroupResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync(
            $"/api/worlds/{worldId}/simulation/advance",
            new { days = 1 },
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<SimulationResultResponse>(cancellationToken: TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();
        payload!.DaysBefore.Should().Be(1);
        payload.DaysAfter.Should().Be(2);
        payload.Settlements.Should().ContainSingle();
        payload.Settlements[0].Prices.Should().ContainSingle(x => x.ProductTypeId == productId && x.Supply == 2m && x.Demand == 5m && x.Price > 10m);
        payload.Settlements[0].Population.Should().Be(50);
    }

    [Fact]
    public async Task ExceptionMiddleware_Should_Return_500_Json()
    {
        await PostgresTestDatabase.ResetAsync();
        using var factory = new TestApiFactory(services =>
        {
            services.RemoveAll<IWorldService>();
            services.AddScoped<IWorldService, ThrowingWorldService>();
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/worlds", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();
        payload!.Error.Should().Be("Internal server error");
        payload.Message.Should().Be("boom");
    }

    private sealed record SimulationResultResponse(int WorldId, int DaysBefore, int DaysAfter, IReadOnlyList<SettlementResultResponse> Settlements);
    private sealed record SettlementResultResponse(int SettlementId, string Name, int Population, IReadOnlyList<InventoryResultResponse> Warehouse, IReadOnlyList<MarketPriceResultResponse> Prices);
    private sealed record InventoryResultResponse(int ProductTypeId, string ProductName, decimal Quantity, string Quality);
    private sealed record MarketPriceResultResponse(int ProductTypeId, string ProductName, decimal Price, decimal Supply, decimal Demand);
    private sealed record SettlementResponse(int SettlementId, string Name, int Population, IReadOnlyList<object> Warehouse, IReadOnlyList<object> Prices);
    private sealed record ErrorResponse(string Error, string Message);

    private sealed class ThrowingWorldService : IWorldService
    {
        public Task<Result<WorldDto>> CreateAsync(string name, string description) => throw new InvalidOperationException("boom");
        public Task<Result<WorldDto>> GetByIdAsync(int id) => throw new InvalidOperationException("boom");
        public Task<Result<IReadOnlyList<WorldDto>>> GetAllAsync() => throw new InvalidOperationException("boom");
        public Task<Result<WorldDto>> UpdateAsync(int id, string name, string description) => throw new InvalidOperationException("boom");
        public Task<Result> DeleteAsync(int id) => throw new InvalidOperationException("boom");
    }
}
