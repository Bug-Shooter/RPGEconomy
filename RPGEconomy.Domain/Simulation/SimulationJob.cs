using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Simulation;

public class SimulationJob : AggregateRoot
{
    public int WorldId { get; private set; }
    public int Days { get; private set; }
    public SimulationJobStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? Error { get; private set; }

    public SimulationJob(
        int id,
        int worldId,
        int days,
        SimulationJobStatus status,
        DateTime createdAtUtc,
        DateTime? startedAtUtc,
        DateTime? completedAtUtc,
        string? error) : base(id)
    {
        WorldId = worldId;
        Days = days;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        Error = error;
    }

    public static SimulationJob Create(int worldId, int days)
        => new(0, worldId, days, SimulationJobStatus.Pending, DateTime.UtcNow, null, null, null);

    public Result MarkRunning()
    {
        if (Status != SimulationJobStatus.Pending)
            return Result.Failure("Задание симуляции уже было запущено");

        Status = SimulationJobStatus.Running;
        StartedAtUtc = DateTime.UtcNow;
        Error = null;
        return Result.Success();
    }

    public Result MarkCompleted()
    {
        if (Status != SimulationJobStatus.Running)
            return Result.Failure("Задание симуляции нельзя завершить из текущего состояния");

        Status = SimulationJobStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        Error = null;
        return Result.Success();
    }

    public Result MarkFailed(string error)
    {
        if (Status != SimulationJobStatus.Running)
            return Result.Failure("Задание симуляции нельзя перевести в ошибку из текущего состояния");

        Status = SimulationJobStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        Error = error;
        return Result.Success();
    }
}
