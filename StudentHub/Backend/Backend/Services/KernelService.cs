using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace Backend.Services;

public class KernelService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly TextEmbeddingService _textEmbeddingService;

    public KernelService(IMemoryCache memoryCache, IConfiguration configuration, TextEmbeddingService textEmbeddingService)
    {
        _memoryCache = memoryCache;
        _configuration = configuration;
        _textEmbeddingService = textEmbeddingService;
    }

    public Task<IKernel> GetKernel(string userId, string studySessionId)
    {
        string apiKey = _configuration.GetConnectionString("OpenAiApiKey");

        return _memoryCache.GetOrCreateAsync($"kernel_{studySessionId}", async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));

            var kernel = new KernelBuilder()
                .WithOpenAIChatCompletionService("gpt-3.5-turbo-16k", apiKey)
                .WithOpenAITextEmbeddingGenerationService("text-embedding-ada-002", apiKey)
                .WithMemoryStorage(new VolatileMemoryStore())
                .Build();

            IEnumerable<Chunk> chunks = await _textEmbeddingService.GetChunks(userId, studySessionId);
            foreach (Chunk chunk in chunks)
                await kernel.Memory.SaveInformationAsync("memory", chunk.Text, chunk.GetHashCode().ToString(),
                    chunk.SourceFile);

            return kernel;
        });
    }

    public void ClearKernel(string studySessionId)
    {
        _memoryCache.Remove($"kernel_{studySessionId}");
    }
}