using StudentHub.Server.Services.AiServices;
using StudentHub.Shared;

namespace StudentHub.Server.Services;

public class ChatService : IChatService
{
    private readonly ChatHistoryService chatHistoryService;
    private readonly List<ChatMessage> messages = new();

    public ChatService(ChatHistoryService memoryCache)
    {
        chatHistoryService = memoryCache;
    }

    public Task<List<ChatMessage>> GetMessagesForUser(string userId)
    {
        return Task.FromResult(messages.Where(m => m.UserId == userId).ToList());
    }

    public Task AddMessage(ChatMessage message)
    {
        if (message.UserId == "Pookie bot")
            chatHistoryService.AddAgentMessage(message.SessionId, message.Message);
        else
            chatHistoryService.AddUserMessage(message.SessionId, message.Message);

        messages.Add(message);
        return Task.CompletedTask;
    }
}