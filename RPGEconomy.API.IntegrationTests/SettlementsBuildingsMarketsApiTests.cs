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
        var recipeId = await seed.CreateRecipeAsync("Bakery", 1, [], [(productId, 1m)]);

        var settlementResponse = await client.PostAsJsonAsync($"/api/worlds/{worldId}/settlements", new { name = "Town" }, cancellationToken: TestContext.Current.CancellationToken);
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
            new { name = "Town" },
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

    [Fact]
    public async Task Settlement_Get_Should_Return_Population_Computed_From_Groups()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var breadId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);

        var settlementResponse = await client.PostAsJsonAsync(
            $"/api/worlds/{worldId}/settlements",
            new { name = "Town" },
            cancellationToken: TestContext.Current.CancellationToken);
        settlementResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var settlement = await settlementResponse.Content.ReadFromJsonAsync<SettlementResponse>(cancellationToken: TestContext.Current.CancellationToken);

        var populationGroupResponse = await client.PostAsJsonAsync(
            $"/api/settlements/{settlement!.SettlementId}/population-groups",
            new
            {
                name = "Peasants",
                populationSize = 75,
                reserveCoverageTicks = 1m,
                consumptionProfile = new[] { new { productTypeId = breadId, amountPerPersonPerTick = 0.1m } }
            },
            cancellationToken: TestContext.Current.CancellationToken);
        populationGroupResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await client.GetAsync(
            $"/api/worlds/{worldId}/settlements/{settlement.SettlementId}",
            TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await getResponse.Content.ReadFromJsonAsync<SettlementResponse>(cancellationToken: TestContext.Current.CancellationToken);
        refreshed.Should().NotBeNull();
        refreshed!.Population.Should().Be(75);
    }

    [Fact]
    public async Task Warehouse_Endpoints_Should_Get_And_Upsert_Stock()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var breadId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);

        var settlementResponse = await client.PostAsJsonAsync(
            $"/api/worlds/{worldId}/settlements",
            new { name = "Town" },
            cancellationToken: TestContext.Current.CancellationToken);
        settlementResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var settlement = await settlementResponse.Content.ReadFromJsonAsync<SettlementResponse>(cancellationToken: TestContext.Current.CancellationToken);
        settlement.Should().NotBeNull();

        var getEmptyResponse = await client.GetAsync(
            $"/api/settlements/{settlement!.SettlementId}/warehouse",
            TestContext.Current.CancellationToken);
        getEmptyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var emptyWarehouse = await getEmptyResponse.Content.ReadFromJsonAsync<List<InventoryItemResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        emptyWarehouse.Should().BeEmpty();

        var createItemResponse = await client.PutAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/warehouse/items",
            new { productTypeId = breadId, quantity = 5m, quality = "Normal" },
            cancellationToken: TestContext.Current.CancellationToken);
        createItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdWarehouse = await createItemResponse.Content.ReadFromJsonAsync<List<InventoryItemResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        createdWarehouse.Should().ContainSingle(item =>
            item.ProductTypeId == breadId &&
            item.Quantity == 5m &&
            item.Quality == "Normal");

        var updateItemResponse = await client.PutAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/warehouse/items",
            new { productTypeId = breadId, quantity = 8m, quality = "Normal" },
            cancellationToken: TestContext.Current.CancellationToken);
        updateItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedWarehouse = await updateItemResponse.Content.ReadFromJsonAsync<List<InventoryItemResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        updatedWarehouse.Should().ContainSingle(item => item.ProductTypeId == breadId && item.Quantity == 8m);

        var removeItemResponse = await client.PutAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/warehouse/items",
            new { productTypeId = breadId, quantity = 0m, quality = "Normal" },
            cancellationToken: TestContext.Current.CancellationToken);
        removeItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var warehouseAfterRemoval = await removeItemResponse.Content.ReadFromJsonAsync<List<InventoryItemResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        warehouseAfterRemoval.Should().BeEmpty();
    }

    [Fact]
    public async Task Warehouse_Endpoints_Should_Return_Expected_Status_Codes_For_Invalid_Input_And_Missing_Entities()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var breadId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);

        var settlementResponse = await client.PostAsJsonAsync(
            $"/api/worlds/{worldId}/settlements",
            new { name = "Town" },
            cancellationToken: TestContext.Current.CancellationToken);
        settlementResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var settlement = await settlementResponse.Content.ReadFromJsonAsync<SettlementResponse>(cancellationToken: TestContext.Current.CancellationToken);
        settlement.Should().NotBeNull();

        var invalidQuantityResponse = await client.PutAsJsonAsync(
            $"/api/settlements/{settlement!.SettlementId}/warehouse/items",
            new { productTypeId = breadId, quantity = -1m, quality = "Normal" },
            cancellationToken: TestContext.Current.CancellationToken);
        invalidQuantityResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var invalidQualityResponse = await client.PutAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/warehouse/items",
            new { productTypeId = breadId, quantity = 1m, quality = "Legendary" },
            cancellationToken: TestContext.Current.CancellationToken);
        invalidQualityResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var missingSettlementResponse = await client.PutAsJsonAsync(
            "/api/settlements/999/warehouse/items",
            new { productTypeId = breadId, quantity = 1m, quality = "Normal" },
            cancellationToken: TestContext.Current.CancellationToken);
        missingSettlementResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var missingProductResponse = await client.PutAsJsonAsync(
            $"/api/settlements/{settlement.SettlementId}/warehouse/items",
            new { productTypeId = 999, quantity = 1m, quality = "Normal" },
            cancellationToken: TestContext.Current.CancellationToken);
        missingProductResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record SettlementResponse(int SettlementId, string Name, int Population, IReadOnlyList<InventoryItemResponse> Warehouse, IReadOnlyList<object> Prices);
    private sealed record BuildingResponse(int Id, string Name, int SettlementId, int RecipeId, int WorkerCount, bool IsActive);
    private sealed record MarketPriceResponse(int ProductTypeId, string ProductName, decimal Price, decimal Supply, decimal Demand);
    private sealed record InventoryItemResponse(int ProductTypeId, string ProductName, decimal Quantity, string Quality);
}
