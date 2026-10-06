namespace SatRural.Application.Modules.Monitoring.Services;

public class SimulationState
{
    private readonly object _lock = new();

    private readonly Dictionary<int, decimal> _sensorValues = new();

    public string Scenario { get; private set; } = "NORMAL";

    public void SetScenario(string scenario)
    {
        lock (_lock)
        {
            Scenario = scenario.ToUpperInvariant();
        }
    }

    public void SetSensorValue(int sensorId, decimal value)
    {
        lock (_lock)
        {
            _sensorValues[sensorId] = value;
        }
    }

    public bool TryGetSensorValue(
        int sensorId,
        out decimal value)
    {
        lock (_lock)
        {
            return _sensorValues.TryGetValue(
                sensorId,
                out value
            );
        }
    }

    public void RemoveSensorValue(int sensorId)
    {
        lock (_lock)
        {
            _sensorValues.Remove(sensorId);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            Scenario = "NORMAL";
            _sensorValues.Clear();
        }
    }
}