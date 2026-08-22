using Microsoft.AspNetCore.SignalR;

using SatRural.Api.Hubs;
using SatRural.Application.Modules.Monitoring.Interfaces;

namespace SatRural.Api.Realtime;

public class SignalRMonitoringNotifier : IMonitoringNotifier
{
    private readonly IHubContext<MonitoringHub> _hubContext;

    public SignalRMonitoringNotifier(
        IHubContext<MonitoringHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyReadingsUpdatedAsync(
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync(
            "SensorReadingsUpdated",
            cancellationToken
        );
    }
}