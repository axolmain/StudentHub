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

        return string.Join("-------------------------\n", history.Select(i => $"{i.Sender}: {i.Message}"));
    }

    private List<ChatEntry>? GetCache(string sessionId)
    {
        return _memoryCache.GetOrCreate($"chat_{sessionId}", entry => { return new List<ChatEntry>(); });
    }

    public void AddAgentMessage(string sessionId, string message)
    {
        GetCache(sessionId).Add(new ChatEntry("bot", message));
    }

    public void AddUserMessage(string sessionId, string message)
    {
        GetCache(sessionId).Add(new ChatEntry("user", message));
    }

    public IEnumerable<ChatEntry> GetMessages(string sessionId)
    {
        return GetCache(sessionId).Select(i => new ChatEntry(i.Sender, i.Message));
    }
}

public class ChatEntry
{
    public ChatEntry(string sender, string message)
    {
        Sender = sender;
        Message = message;
    }

    public string Sender { get; set; }
    public string Message { get; set; }
}