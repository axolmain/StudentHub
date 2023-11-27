using Microsoft.AspNetCore.SignalR;

namespace StudentHub.Server;

public class ReportHub : Hub
{
    public async Task SendProgress(string message)
    {
        await Clients.All.SendAsync("ReceiveProgress", message);
    }
}