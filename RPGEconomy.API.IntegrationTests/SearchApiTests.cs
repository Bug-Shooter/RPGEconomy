using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RPGEconomy.Testing;

namespace RPGEconomy.API.IntegrationTests;

[Collection(IntegrationTestCollection.Database)]
public class SearchApiTests : IAsyncLifetime
{
    private readonly TestApiFactory _factory = new();

    public ValueTask InitializeAsync() => new(PostgresTestDatabase.ResetAsync());
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Product_Search_Should_Filter_By_Name_And_Fallback_On_Whitespace()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        await seed.CreateProductTypeAsync("Bread", "Desc", 10m, 1);
        await seed.CreateProductTypeAsync("Brew", "Desc", 10m, 1);
        await seed.CreateProductTypeAsync("Wood", "Desc", 10m, 1);

        var searchResponse = await client.GetAsync("/api/products?search=bre", TestContext.Current.CancellationToken);

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchResults = await searchResponse.Content.ReadFromJsonAsync<List<ProductTypeResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        searchResults.Should().NotBeNull();
        searchResults!.Select(x => x.Name).Should().BeEquivalentTo(["Bread", "Brew"]);

        var allResponse = await client.GetAsync("/api/products?search=%20%20%20", TestContext.Current.CancellationToken);
        allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allResults = await allResponse.Content.ReadFromJsonAsync<List<ProductTypeResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        allResults.Should().HaveCount(3);
    }

    [Fact]
    public async Task Resource_Search_Should_Be_Case_Insensitive()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        await seed.CreateResourceTypeAsync("Wood", "Desc", true, 1);
        await seed.CreateResourceTypeAsync("Hardwood", "Desc", true, 1);
        await seed.CreateResourceTypeAsync("Stone", "Desc", false, 0);

        var response = await client.GetAsync("/api/resources?search=WOOD", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ResourceTypeResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        results.Should().NotBeNull();
        results!.Select(x => x.Name).Should().BeEquivalentTo(["Wood", "Hardwood"]);
    }

    [Fact]
    public async Task Recipe_Search_Should_Filter_By_Name_And_Return_Empty_List_When_No_Matches()
    {
        var client = _factory.CreateClient();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var productId = await seed.CreateProductTypeAsync("Bread", "Desc", 10m, 1);
        await seed.CreateRecipeAsync("Broth", 1, [], [(productId, 1m)]);
        await seed.CreateRecipeAsync("Brown Bread", 1, [], [(productId, 1m)]);
        await seed.CreateRecipeAsync("Bakery", 1, [], [(productId, 1m)]);

        var response = await client.GetAsync("/api/recipes?search=bro", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<RecipeResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        results.Should().NotBeNull();
        results!.Select(x => x.Name).Should().BeEquivalentTo(["Broth", "Brown Bread"]);

        var emptyResponse = await client.GetAsync("/api/recipes?search=zzz", TestContext.Current.CancellationToken);
        emptyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var emptyResults = await emptyResponse.Content.ReadFromJsonAsync<List<RecipeResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        emptyResults.Should().BeEmpty();
    }

    private sealed record ProductTypeResponse(int Id, string Name, string Description, decimal BasePrice, double WeightPerUnit);
    private sealed record ResourceTypeResponse(int Id, string Name, string Description, bool IsRenewable, double RegenerationRatePerDay);
    private sealed record RecipeIngredientResponse(int ProductTypeId, decimal Quantity);
    private sealed record RecipeResponse(int Id, string Name, double LaborDaysRequired, IReadOnlyList<RecipeIngredientResponse> Inputs, IReadOnlyList<RecipeIngredientResponse> Outputs);
}
