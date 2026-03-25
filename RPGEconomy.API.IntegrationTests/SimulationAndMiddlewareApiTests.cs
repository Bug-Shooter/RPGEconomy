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

    private sealed record SimulationResultResponse(int WorldId, int DaysBefore, int DaysAfter, IReadOnlyList<object> Settlements);
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
