using StudentHub.Shared;

namespace StudentHub.Server.Services;

public interface IChatService
{
    public Task<List<ChatMessage>> GetMessagesForUser(string userId);
    public Task AddMessage(ChatMessage message);
}