using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Memory.Weaviate;


namespace StudentHub.Server.Services;

public class KernelService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly TextEmbeddingService _textEmbeddingService;
    private WeaviateMemoryStore memoryStore = new("http://31.220.108.226:8080");

    public KernelService(IMemoryCache memoryCache, IConfiguration configuration, TextEmbeddingService textEmbeddingService)
    {
        _memoryCache = memoryCache;
        _configuration = configuration;
        _textEmbeddingService = textEmbeddingService;
    }

    public Task<IKernel> GetKernel(string userId, string studySessionId)
    {
        string apiKey = _configuration.GetConnectionString("OpenAiApiKey");

        return _memoryCache.GetOrCreateAsync<IKernel>($"kernel_{studySessionId}", async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));

            var kernel = new KernelBuilder()
                .WithOpenAIChatCompletionService("gpt-3.5-turbo-16k", apiKey)
                .WithOpenAITextEmbeddingGenerationService("text-embedding-ada-002", apiKey)
                .WithMemoryStorage(memoryStore)
                .Build();

            IEnumerable<Chunk> chunks = await _textEmbeddingService.GetChunks(userId, studySessionId);
            foreach (Chunk chunk in chunks)
                await kernel.Memory.SaveInformationAsync("memory", chunk.Text, chunk.GetHashCode().ToString(),
                    chunk.SourceFile);

            return kernel;
        });
    }
}