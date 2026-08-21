using Microsoft.AspNetCore.SignalR;

namespace SatRural.Api.Hubs;

public class AlertHub : Hub
{
    public async Task BroadcastAlert(string riskLevel, string phenomenon, string description)
    {
        await Clients.All.SendAsync("ReceiveAlert", riskLevel, phenomenon, description);
    }
}