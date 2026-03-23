using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Simulation;
using RPGEconomy.Infrastructure.Persistence.Queries;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class SimulationJobRepository : ISimulationJobRepository
{
    private readonly IDbConnectionFactory _factory;

    public SimulationJobRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<SimulationJob?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<SimulationJob>(
            SimulationJobQueries.GetById,
            new { Id = id });
    }

    public async Task<int> SaveAsync(SimulationJob job)
    {
        using var conn = _factory.Create();

        if (job.IsNew)
        {
            return await conn.ExecuteScalarAsync<int>(SimulationJobQueries.Insert, new
            {
                job.WorldId,
                job.Days,
                Status = (int)job.Status,
                job.CreatedAtUtc,
                job.StartedAtUtc,
                job.CompletedAtUtc,
                job.Error
            });
        }

        await conn.ExecuteAsync(SimulationJobQueries.Update, new
        {
            job.Id,
            job.WorldId,
            job.Days,
            Status = (int)job.Status,
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.Error
        });

        return job.Id;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(SimulationJobQueries.Delete, new { Id = id });
    }
}
