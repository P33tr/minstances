using Microsoft.AspNetCore.SignalR;

namespace minstances.Hubs;

public class StatusHub : Hub
{
    public async Task Send(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}