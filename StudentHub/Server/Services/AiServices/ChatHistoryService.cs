using Microsoft.Extensions.Caching.Memory;

namespace StudentHub.Server.Services.AiServices;

public class ChatHistoryService
{
    private readonly IMemoryCache _memoryCache;

    public ChatHistoryService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public string GetChatHistory(string sessionId)
    {
        var history = GetCache(sessionId);

        return string.Join("-------------------------\n", history.Select(i => $"{i.sender}: {i.message}"));
    }

    private List<ChatEntry> GetCache(string sessionId)
    {
        return _memoryCache.GetOrCreate($"chat_{sessionId}", entry =>
        {
            return new List<ChatEntry>();
        });
    }

    public void AddAgentMessage(string sessionId, string message)
    {
        GetCache(sessionId).Add(new ChatEntry
        {
            sender = "bot",
            message = message
        });
    }

    public void AddUserMessage(string sessionId, string message)
    {
        GetCache(sessionId).Add(new ChatEntry
        {
            sender = "user",
            message = message
        });
    }

    public IEnumerable<ChatEntry> GetMessages(string sessionId)
    {
        return GetCache(sessionId).Select(i => new ChatEntry
        {
            sender = i.sender,
            message = i.message,
        });
    }
}

public class ChatEntry
{
    public string sender { get; set; }
    public string message { get; set; }
}