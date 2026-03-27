namespace RPGEconomy.Simulation.Engine;

public sealed record DemandRequest(
    int OwnerId,
    int ProductTypeId,
    decimal Quantity);
