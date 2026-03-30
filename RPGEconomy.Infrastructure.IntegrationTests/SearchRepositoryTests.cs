using Dapper;
using FluentAssertions;
using RPGEconomy.Infrastructure.Persistence;
using RPGEconomy.Infrastructure.Persistence.Repositories;
using RPGEconomy.Testing;

namespace RPGEconomy.Infrastructure.IntegrationTests;

[Collection(IntegrationTestCollection.Database)]
public class SearchRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public SearchRepositoryTests(DatabaseFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task Database_Should_Enable_PgTrgm_Extension()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();

        var installed = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm');");

        installed.Should().BeTrue();
    }

    [Fact]
    public async Task ProductTypeRepository_SearchByNameAsync_Should_Find_By_Substring_And_Order_Deterministically()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        await seed.CreateProductTypeAsync("Ore", "Desc", 1m, 1);
        await seed.CreateProductTypeAsync("Ore A", "Desc", 1m, 1);
        await seed.CreateProductTypeAsync("Ore B", "Desc", 1m, 1);
        await seed.CreateProductTypeAsync("Wood", "Desc", 1m, 1);

        var repository = new ProductTypeRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));

        var results = await repository.SearchByNameAsync("ore");

        results.Select(x => x.Name).Should().Equal("Ore", "Ore A", "Ore B");
    }

    [Fact]
    public async Task ResourceTypeRepository_SearchByNameAsync_Should_Find_By_Substring_And_Order_Deterministically()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        await seed.CreateResourceTypeAsync("Ore", "Desc", false, 0);
        await seed.CreateResourceTypeAsync("Ore A", "Desc", false, 0);
        await seed.CreateResourceTypeAsync("Ore B", "Desc", false, 0);
        await seed.CreateResourceTypeAsync("Wood", "Desc", true, 1);

        var repository = new ResourceTypeRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));

        var results = await repository.SearchByNameAsync("ORE");

        results.Select(x => x.Name).Should().Equal("Ore", "Ore A", "Ore B");
    }

    [Fact]
    public async Task ProductionRecipeRepository_SearchByNameAsync_Should_Find_By_Substring_Load_Ingredients_And_Order_Deterministically()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var inputId = await seed.CreateProductTypeAsync("Grain", "Desc", 1m, 1);
        var outputId = await seed.CreateProductTypeAsync("Bread", "Desc", 2m, 1);
        await seed.CreateRecipeAsync("Bakery", 1, [(inputId, 1m)], [(outputId, 1m)]);
        await seed.CreateRecipeAsync("Bakery A", 1, [(inputId, 2m)], [(outputId, 1m)]);
        await seed.CreateRecipeAsync("Bakery B", 1, [(inputId, 3m)], [(outputId, 1m)]);
        await seed.CreateRecipeAsync("Mill", 1, [(inputId, 1m)], [(outputId, 1m)]);

        var repository = new ProductionRecipeRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));

        var results = await repository.SearchByNameAsync("bakery");

        results.Select(x => x.Name).Should().Equal("Bakery", "Bakery A", "Bakery B");
        results.Should().OnlyContain(x => x.Inputs.Count == 1 && x.Outputs.Count == 1);
        results.Select(x => x.Inputs.Single().Quantity).Should().Equal(1m, 2m, 3m);
    }
}
