using Microsoft.AspNetCore.SignalR;

namespace minstances.Hubs;

public class StatusHub : Hub
{
    public async Task SendAsync(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}