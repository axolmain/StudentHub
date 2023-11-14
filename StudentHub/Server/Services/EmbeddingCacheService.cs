using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel.Memory;

namespace StudentHub.Server.Services;

public class EmbeddingCacheService
{
    private readonly IMemoryCache _memoryCache;

    public EmbeddingCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public bool TryGetEmbeddings(string studySessionId, out ISemanticTextMemory embeddings)
    {
        return _memoryCache.TryGetValue(GetCacheKey(studySessionId), out embeddings);
    }

    public void SetEmbeddings(string studySessionId, ISemanticTextMemory embeddings)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            // Set your cache expiration preferences here
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        _memoryCache.Set(GetCacheKey(studySessionId), embeddings, cacheEntryOptions);
    }

    private string GetCacheKey(string studySessionId)
    {
        return $"Embeddings-{studySessionId}";
    }
}