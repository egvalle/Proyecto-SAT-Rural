namespace SatRural.Application.Modules.Monitoring.Services;

public class SimulationState
{
    public string Scenario { get; private set; } = "NORMAL";

    public void SetScenario(string scenario)
    {
        Scenario = scenario.ToUpperInvariant();
    }

    public void Reset()
    {
        Scenario = "NORMAL";
    }
}