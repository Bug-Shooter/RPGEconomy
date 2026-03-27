using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RPGEconomy.Testing;

namespace RPGEconomy.API.IntegrationTests;

[Collection(IntegrationTestCollection.Database)]
public class SettlementsBuildingsMarketsApiTests : IAsyncLifetime
{
    private readonly TestApiFactory _factory = new();

    public ValueTask InitializeAsync() => new(PostgresTestDatabase.ResetAsync());
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Settlement_Building_And_Market_Endpoints_Should_Work_EndToEnd()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var productId = await seed.CreateProductTypeAsync("Bread", "Desc", 10m, 1);
        var recipeId = await seed.CreateRecipeAsync("Bakery", 1, [], [(productId, 1)]);

        var settlementResponse = await client.PostAsJsonAsync($"/api/worlds/{worldId}/settlements", new { name = "Town", population = 200 }, cancellationToken: TestContext.Current.CancellationToken);
        settlementResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var settlement = await settlementResponse.Content.ReadFromJsonAsync<SettlementResponse>(cancellationToken: TestContext.Current.CancellationToken);
        settlement.Should().NotBeNull();

        var buildingResponse = await client.PostAsJsonAsync($"/api/settlements/{settlement!.SettlementId}/buildings", new { name = "Bakery", recipeId, workerCount = 2 }, cancellationToken: TestContext.Current.CancellationToken);
        buildingResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var building = await buildingResponse.Content.ReadFromJsonAsync<BuildingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        building.Should().NotBeNull();

        var deactivateResponse = await client.PatchAsync($"/api/settlements/{settlement.SettlementId}/buildings/{building!.Id}/deactivate", null, TestContext.Current.CancellationToken);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var marketResponse = await client.PostAsJsonAsync($"/api/settlements/{settlement.SettlementId}/market/products", new { productTypeId = productId, initialPrice = 12.5m }, cancellationToken: TestContext.Current.CancellationToken);
        marketResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/market/products/{productId}",
            new { supply = 4, demand = 9 },
            cancellationToken: TestContext.Current.CancellationToken);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MarketPriceResponse>(cancellationToken: TestContext.Current.CancellationToken);
        updated.Should().NotBeNull();
        updated!.Supply.Should().Be(4);
        updated.Demand.Should().Be(9);

        var productResponse = await client.GetAsync($"/api/settlements/{settlement.SettlementId}/market/products/{productId}", TestContext.Current.CancellationToken);
        productResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await productResponse.Content.ReadFromJsonAsync<MarketPriceResponse>(cancellationToken: TestContext.Current.CancellationToken);
        product.Should().NotBeNull();
        product!.ProductTypeId.Should().Be(productId);

        var pricesResponse = await client.GetAsync($"/api/settlements/{settlement.SettlementId}/market/prices", TestContext.Current.CancellationToken);
        pricesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var prices = await pricesResponse.Content.ReadFromJsonAsync<List<MarketPriceResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        prices.Should().NotBeNull();
        prices!.Should().ContainSingle(x => x.ProductTypeId == productId);
    }

    [Fact]
    public async Task Market_Endpoints_Should_Return_Expected_Status_Codes_For_Invalid_Input_And_Missing_Product()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var productId = await seed.CreateProductTypeAsync("Bread", "Desc", 10m, 1);

        var settlementResponse = await client.PostAsJsonAsync(
            $"/api/worlds/{worldId}/settlements",
            new { name = "Town", population = 200 },
            cancellationToken: TestContext.Current.CancellationToken);
        var settlement = await settlementResponse.Content.ReadFromJsonAsync<SettlementResponse>(cancellationToken: TestContext.Current.CancellationToken);

        var invalidRegister = await client.PostAsJsonAsync(
            $"/api/settlements/{settlement!.SettlementId}/market/products",
            new { productTypeId = productId, initialPrice = 0m },
            cancellationToken: TestContext.Current.CancellationToken);
        invalidRegister.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var missingProduct = await client.GetAsync(
            $"/api/settlements/{settlement.SettlementId}/market/products/999",
            TestContext.Current.CancellationToken);
        missingProduct.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var validRegister = await client.PostAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/market/products",
            new { productTypeId = productId, initialPrice = 10m },
            cancellationToken: TestContext.Current.CancellationToken);
        validRegister.StatusCode.Should().Be(HttpStatusCode.OK);

        var invalidUpdate = await client.PutAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/market/products/{productId}",
            new { supply = -1, demand = 3 },
            cancellationToken: TestContext.Current.CancellationToken);
        invalidUpdate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record SettlementResponse(int SettlementId, string Name, int Population, IReadOnlyList<object> Warehouse, IReadOnlyList<object> Prices);
    private sealed record BuildingResponse(int Id, string Name, int SettlementId, int RecipeId, int WorkerCount, bool IsActive);
    private sealed record MarketPriceResponse(int ProductTypeId, string ProductName, decimal Price, int Supply, int Demand);
}
