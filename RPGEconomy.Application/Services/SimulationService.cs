using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Simulation;

namespace RPGEconomy.Application.Services;

public class SimulationService : ISimulationService
{
    private readonly ISimulationExecutor _simulationExecutor;
    private readonly ISimulationJobRepository _simulationJobRepository;

    public SimulationService(
        ISimulationExecutor simulationExecutor,
        ISimulationJobRepository simulationJobRepository)
    {
        _simulationExecutor = simulationExecutor;
        _simulationJobRepository = simulationJobRepository;
    }

    public async Task<Result<SimulationResultDto>> AdvanceAsync(
        RunSimulationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.WorldId <= 0)
            return Result<SimulationResultDto>.Failure("Идентификатор мира должен быть больше нуля");

        if (command.Days <= 0)
            return Result<SimulationResultDto>.Failure("Количество дней должно быть больше нуля");

        var job = SimulationJob.Create(command.WorldId, command.Days);
        var jobId = await _simulationJobRepository.SaveAsync(job);
        job = await _simulationJobRepository.GetByIdAsync(jobId)
            ?? throw new InvalidOperationException("Созданное задание симуляции не найдено");

        var startResult = job.MarkRunning();
        if (!startResult.IsSuccess)
            return Result<SimulationResultDto>.Failure(startResult.Error!);

        await _simulationJobRepository.SaveAsync(job);

        var executionRequest = new SimulationExecutionRequest(job.Id, command.WorldId, command.Days);
        Result<SimulationExecutionResult> executionResult;

        try
        {
            executionResult = await _simulationExecutor.ExecuteAsync(executionRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            var failUnexpectedResult = job.MarkFailed(ex.Message);
            if (failUnexpectedResult.IsSuccess)
                await _simulationJobRepository.SaveAsync(job);

            throw;
        }

        if (executionResult.IsSuccess)
        {
            var completeResult = job.MarkCompleted();
            if (!completeResult.IsSuccess)
                return Result<SimulationResultDto>.Failure(completeResult.Error!);

            await _simulationJobRepository.SaveAsync(job);
            return Result<SimulationResultDto>.Success(executionResult.Value!.Result);
        }

        var failResult = job.MarkFailed(executionResult.Error ?? "Ошибка выполнения симуляции");
        if (!failResult.IsSuccess)
            return Result<SimulationResultDto>.Failure(failResult.Error!);

        await _simulationJobRepository.SaveAsync(job);
        return Result<SimulationResultDto>.Failure(executionResult.Error ?? "Ошибка выполнения симуляции");
    }
}
