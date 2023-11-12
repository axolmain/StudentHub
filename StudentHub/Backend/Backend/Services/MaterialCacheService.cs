using Backend.Services.AiServices;

using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services;

public class MaterialCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly FlashcardService _flashcardService;
    private readonly MultipleChoiceService _multipleChoiceService;

    public MaterialCacheService(IMemoryCache memoryCache, FlashcardService flashcardService, MultipleChoiceService multipleChoiceService)
    {
        _memoryCache = memoryCache;
        _flashcardService = flashcardService;
        _multipleChoiceService = multipleChoiceService;
    }

    public Task<string> GetFlashCards(string sessionId)
    {
        return _memoryCache.GetOrCreateAsync($"fc_{sessionId}", entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(20));

            return _flashcardService.Execute(sessionId);
        });
    }

    public Task<string> GetQuiz(string sessionId)
    {
        return _memoryCache.GetOrCreateAsync($"mc_{sessionId}", entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(20));

            return _multipleChoiceService.Execute(sessionId);
        });
    }
}
