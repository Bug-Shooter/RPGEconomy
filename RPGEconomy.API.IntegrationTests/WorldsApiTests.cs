using FluentAssertions;
using RPGEconomy.Testing;
using System.Net;
using System.Net.Http.Json;

namespace RPGEconomy.API.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class WorldsApiTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly TestApiFactory _factory = new();

    public WorldsApiTests(DatabaseFixture fixture)
        => _fixture = fixture;

    public ValueTask InitializeAsync() =>
        new(PostgresTestDatabase.ResetAsync());

    public ValueTask DisposeAsync() =>
        new(Task.CompletedTask);

    [Fact]
    public async Task Post_Get_Delete_World_Should_Work_EndToEnd()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/worlds", new { name = "Earth", description = "Desc" }, cancellationToken: TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<WorldResponse>(cancellationToken: TestContext.Current.CancellationToken);
        created.Should().NotBeNull();

        var getResponse = await client.GetAsync($"/api/worlds/{created!.Id}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/worlds/{created.Id}", TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_World_Should_Return_BadRequest_For_Empty_Name()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/worlds", new { name = "", description = "Desc" }, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record WorldResponse(int Id, string Name, string Description, int CurrentDay);
}
