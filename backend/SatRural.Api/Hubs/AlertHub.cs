using Microsoft.AspNetCore.SignalR;

namespace SatRural.Api.Hubs;

public class AlertHub : Hub
{
    public async Task BroadcastAlert(
        string level,
        string type,
        string message)
    {
        await Clients.All.SendAsync(
            "ReceiveAlert",
            level,
            type,
            message
        );
    }
}