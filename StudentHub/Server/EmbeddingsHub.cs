using Microsoft.AspNetCore.SignalR;

namespace StudentHub.Server;

public class EmbeddingsHub : Hub
{
    public async Task NotifyEmbeddingsGenerated(string userId)
    {
        await Clients.User(userId).SendAsync("EmbeddingsGenerated");
    }
}
