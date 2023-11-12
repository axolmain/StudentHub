using StudentHub.Shared;

namespace StudentHub.Server.Services;

public class ChatService : IChatService
{
    private readonly List<ChatMessage> messages = new();

    public Task<List<ChatMessage>> GetMessagesForUser(string userId)
    {
        return Task.FromResult(messages.Where(m => m.UserId == userId).ToList());
    }

    public Task AddMessage(ChatMessage message)
    {
        messages.Add(message);
        return Task.CompletedTask;
    }
}