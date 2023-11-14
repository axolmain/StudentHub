using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Memory.Weaviate;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Plugins.Memory;


namespace StudentHub.Server.Services;

public class KernelService
{
    private readonly IMemoryCache _memoryCache;
    private readonly TextEmbeddingService textEmbeddingService;
    //private readonly VolatileMemoryStore memoryStore = new();
    private readonly string? _openApiKey;

    public KernelService(IMemoryCache memoryCache, IConfiguration configuration, TextEmbeddingService textEmbeddingService)
    {
        _memoryCache = memoryCache;
        _openApiKey = configuration.GetConnectionString("OpenAiApiKey");

        this.textEmbeddingService = textEmbeddingService;
    }

    public async Task<IKernel?> GetKernelAsync(string userId, string studySessionId)
        {
            return await _memoryCache.GetOrCreateAsync($"kernel_{studySessionId}", async entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                var kernel = new KernelBuilder()
                    .WithOpenAITextCompletionService("gpt-3.5-turbo-16k", _openApiKey)
                    .WithOpenAITextEmbeddingGenerationService("text-embedding-ada-002", _openApiKey)
                    .Build();
                
                // WeaviateMemoryStore memoryStore = new("http://31.220.108.226:8080");
                VolatileMemoryStore memoryStore = new();
                var embeddingGenerator = new OpenAITextEmbeddingGeneration(modelId: "text-embedding-ada-002", _openApiKey);
            
                var textMemory = new MemoryBuilder()
                    .WithTextEmbeddingGeneration(embeddingGenerator)
                    .WithMemoryStore(memoryStore)
                    .Build();

                string pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Services/AiServices/plugins/");
                var orchestratorPlugin = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "OrchestratorPlugin");

                var memoryPlugin = new TextMemoryPlugin(textMemory);
                var memoryFunctions = kernel.ImportFunctions(memoryPlugin, "MemoryPlugin");

                string memoryCollectionName = $"Testing/{userId}/{studySessionId}";
                
                if (await memoryStore.DoesCollectionExistAsync(memoryCollectionName)) 
                    return kernel;
                
                await memoryStore.CreateCollectionAsync(memoryCollectionName);
                IEnumerable<Chunk> chunks = await textEmbeddingService.GetChunks(userId, studySessionId);
                
                foreach (Chunk chunk in chunks)
                    await SaveToNewCollectionAsync(kernel, memoryFunctions, memoryCollectionName, chunk.GetHashCode().ToString(), chunk.Text);
                kernel.RegisterMemory(textMemory);
                return kernel;
        });
    }
    
    
    
    private static async Task<IEnumerable<KernelResult?>> SaveToNewCollectionAsync(IKernel kernel, IDictionary<string, ISKFunction> memoryFunctions, 
        string collectionName, string chunkText, string chunkHashCode)
    {
        var results = new List<KernelResult?>();
        var result = await kernel.RunAsync(memoryFunctions["Save"], new ContextVariables
        {
                [TextMemoryPlugin.CollectionParam] = collectionName,
                [TextMemoryPlugin.KeyParam] = chunkHashCode,
                ["input"] = chunkText
            });
            results.Add(result);
        return results;
    }

    public async Task<string> RunChat(ISemanticTextMemory textMemory, string collectionName, string userQuestion, string userId, string sessionId)
    {
        IKernel? kernel = await GetKernelAsync(userId, sessionId);
        string answer = await ApplyRAG(textMemory, kernel, collectionName, userQuestion);
        return answer;
    }
    
    private static async Task<string> ApplyRAG(ISemanticTextMemory memory, IKernel kernel, string collectionName, string userQuestion)
    {

        var context = kernel.CreateNewContext();
        context.Variables.Add("QUESTION", userQuestion);

        // retrieve user-specific context based on the user input
        var searchResults = memory.SearchAsync(collectionName, userQuestion);
        var retrieved = new List<string>();
        await foreach (var item in searchResults)
        {
            retrieved.Add(item.Metadata.Text);
        }
        context.Variables.Add("CONTEXT", string.Join(Environment.NewLine, retrieved));

        // run SK function and give it the two variables, input and context
        var func = kernel.Functions.GetFunction("OrchestratorPlugin", "AskAboutDocument");
        var answer = await kernel.RunAsync(func, context.Variables);

        return answer.GetValue<string>()!.Trim();

    }
    
    public async Task SaveMemoryAsync(IKernel? kernel, string memoryCollectionName, string id, string text, string sourceFile = "")
    {
        await kernel.Memory.SaveInformationAsync(memoryCollectionName, id, text, sourceFile);
    }


    public async Task<string?> RetrieveMemoryAsync(IKernel kernel, string memoryCollectionName, string userQuestion)
    {
        string result = string.Empty;
        await foreach (var memory in kernel.Memory.SearchAsync(memoryCollectionName, userQuestion, 5)) 
            result += $"\r\n\r\n{memory.Metadata.Text} found in {memory.Metadata.Description}";

        return result;
    }
    
    // public async Task<string> ApplyRAGWrong(string userId, string sessionId, string history, 
    //     string context, string userQuestion)
    // {
    //     var kernel = GetKernelAsync(userId, sessionId);
    //     string pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Services/AiServices/plugins/");
    //     var orchestratorPlugin = kernel.ImportSemanticSkillFromDirectory(pluginsDirectory, "OrchestratorPlugin");
    //
    //     // Prepare the variables for the function invocation.
    //     var variables = new ContextVariables
    //     {
    //         ["HISTORY"] = history,
    //         ["QUESTION"] = userQuestion,
    //         ["CONTEXT"] = context
    //     };
    //
    //     // Invoke the function and obtain the result.
    //     var response = await kernel.RunAsync(variables, orchestratorPlugin["AskAboutDocument"]);
    //     string result = response.GetValue<string>();
    //
    //     return result;
    // }
}