using Microsoft.AspNetCore.SignalR;
using CanTelemetryApp.Models;

namespace CanTelemetryApp.Hubs;

public class TelemetryHub : Hub
{
    // Frontend bu fonksiyonu dinleyecek
    public async Task BroadcastStats(VehicleStatsDto dto)
    {
        await Clients.All.SendAsync("TelemetryUpdated", dto);
    }
}