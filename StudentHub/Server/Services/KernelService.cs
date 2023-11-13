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
    private readonly IMemoryCache memoryCache;
    private readonly IConfiguration configuration;
    private readonly TextEmbeddingService textEmbeddingService;
    private readonly WeaviateMemoryStore memoryStore = new("http://31.220.108.226:8080");
    //private readonly VolatileMemoryStore memoryStore = new();
    private readonly string? openApiKey;

    public KernelService(IMemoryCache memoryCache, IConfiguration configuration, TextEmbeddingService textEmbeddingService, MemoryBuilder memoryBuilder, IDictionary<string, ISKFunction> memoryFunctions)
    {
        this.memoryCache = memoryCache;
        this.configuration = configuration;
        openApiKey = configuration.GetConnectionString("OpenAiApiKey");

        this.textEmbeddingService = textEmbeddingService;
        this.memoryFunctions = memoryFunctions;
    }

    public Task<IKernel?> GetKernel(string userId, string studySessionId)
    {

        return memoryCache.GetOrCreateAsync<IKernel>($"kernel_{studySessionId}", async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));
            var kernel = new KernelBuilder()
                .WithOpenAIChatCompletionService("gpt-3.5-turbo-16k", openApiKey)
                .Build();
            
            var embeddingGenerator = new OpenAITextEmbeddingGeneration("text-embedding-ada-002", openApiKey);
            SemanticTextMemory textMemory = new(memoryStore, embeddingGenerator);
            
            // Import the TextMemoryPlugin into the Kernel for other functions
            var memoryPlugin = new TextMemoryPlugin(textMemory);
            memoryFunctions = kernel.ImportFunctions(memoryPlugin);
            var memoryWithCustomDb = new MemoryBuilder()
                .WithOpenAITextEmbeddingGenerationService("text-embedding-ada-002", openApiKey)
                .WithMemoryStore(memoryStore)
                .Build();
                
            IEnumerable<Chunk> chunks = await textEmbeddingService.GetChunks(userId, studySessionId);
            foreach (Chunk chunk in chunks)
                await SaveMemoryAsync(kernel, $"Testing/{userId}/{studySessionId}", chunk.GetHashCode().ToString(), chunk.Text);

            return kernel;
        });
    }


    private IDictionary<string, ISKFunction> memoryFunctions;

    public async Task SaveMemoryAsync(IKernel? kernel, string memoryCollectionName, string text, string id,
        string sourceFile = "")
    {
        kernel.Memory.SaveInformationAsync(memoryCollectionName, text, id, sourceFile);
        // await kernel.Functions.GetFunction("Save").InvokeAsync(kernel, new ContextVariables
        // {
        //     [TextMemoryPlugin.CollectionParam] = memoryCollectionName,
        //     [TextMemoryPlugin.KeyParam] = id,
        //     ["input"] = text,
        //     ["sourceFile"] = sourceFile
        // });
    }


    public async Task<string?> RetrieveMemoryAsync(IKernel? kernel, string memoryCollectionName, string userQuestion)
    {
        // FunctionResult results = await kernel.Functions.GetFunction("Recall").InvokeAsync(kernel, new ContextVariables
        // {
        //     [TextMemoryPlugin.CollectionParam] = memoryCollectionName,
        //     [TextMemoryPlugin.LimitParam] = "5",
        //     [TextMemoryPlugin.RelevanceParam] = "0.5",
        //     ["input"] = $"Ask: {userQuestion}"
        // });
        //
        // return results.GetValue<string>();

        string result = string.Empty;
        await foreach (MemoryQueryResult memory in kernel.Memory.SearchAsync(memoryCollectionName, userQuestion, 5, 0.5)) 
            result = $"{result}\r\n \r\n{memory.Metadata.Text} found in {memory.Metadata.Description}";
        
        return result;
    }
    
    public async Task<string> AskAboutDocumentsAsync(IKernel? kernel, string history, string userQuestion, string context)
    {
        string pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Services/AiServices/plugins/");
        var orchestratorPlugin = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "OrchestratorPlugin");

        // Prepare the variables for the function invocation.
        var variables = new ContextVariables
        {
            ["HISTORY"] = history,
            ["QUESTION"] = userQuestion,
            ["CONTEXT"] = context
        };

        // Invoke the function and obtain the result.
        var response = await kernel.RunAsync(variables, orchestratorPlugin["AskAboutDocument"]);
        string result = response.FunctionResults.First().ToString();

        return result;
    }
}