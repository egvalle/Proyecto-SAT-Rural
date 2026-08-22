namespace SatRural.Application.Modules.Monitoring.Interfaces;

public interface IMonitoringNotifier
{
    Task NotifyReadingsUpdatedAsync(
        CancellationToken cancellationToken = default
    );
}