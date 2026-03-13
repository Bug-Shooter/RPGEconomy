namespace RPGEconomy.Simulation.Engine;

public class SimulationClock
{
    public int CurrentDay { get; private set; }

    public SimulationClock(int currentDay)
        => CurrentDay = currentDay;

    public void Advance(int days)
    {
        if (days <= 0) throw new ArgumentException("Количество дней должно быть > 0");
        CurrentDay += days;
    }
}
