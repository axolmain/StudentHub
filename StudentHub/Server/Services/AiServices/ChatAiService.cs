using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;

namespace StudentHub.Server.Services.AiServices;

public class ChatAiService
{
    private readonly EmbeddingCacheService _embeddingCacheService;
    private readonly ChatHistoryService _chatHistoryService;
    private readonly KernelService _kernelService;
    private static TextEmbeddingService? _textEmbeddingService;
    private ISKFunction _chatFunction;

    private IKernel _kernel;
    private readonly IUserAuthService _userAuthService;

    public ChatAiService(IConfiguration configuration, EmbeddingCacheService embeddingCacheService, 
        ChatHistoryService chatHistoryService,
        KernelService kernelService, TextEmbeddingService? textEmbeddingService, IUserAuthService userAuthService)
    {
        _embeddingCacheService = embeddingCacheService;
        _chatHistoryService = chatHistoryService;
        _kernelService = kernelService;
        _textEmbeddingService = textEmbeddingService;
        _userAuthService = userAuthService;
    }

    public async Task<string> Execute(string userQuestion, string studySessionId)
    {
        string? userId = studySessionId;
        _kernel = await _kernelService.GetKernel(userId, studySessionId);
        _chatFunction = RegisterChatFunction(_kernel);
        string memoryCollectionName = $"{userId}/{studySessionId}";
        await RefreshMemory(_kernel, userId, studySessionId, memoryCollectionName);
        string responses = await GetChatResponse(_kernel, _chatFunction, userQuestion, memoryCollectionName, studySessionId);

        return responses;
    }

    private async Task RefreshMemory(IKernel kernel, string? userId, string studySessionId,
        string memoryCollectionName)
    {
        if (_embeddingCacheService.TryGetEmbeddings(userId, out ISemanticTextMemory cachedEmbeddings ))
        {
            kernel.RegisterMemory(cachedEmbeddings);
            return;
        }
        IEnumerable<Chunk> chunks = await _textEmbeddingService.GetChunks(userId, studySessionId);
        foreach (Chunk chunk in chunks)
            await kernel.Memory.SaveInformationAsync(memoryCollectionName, chunk.Text, chunk.GetHashCode().ToString(),
                chunk.SourceFile);
        _embeddingCacheService.SetEmbeddings(studySessionId, kernel.Memory);
    }

    private static ISKFunction RegisterChatFunction(IKernel kernel)
    {
        const string skPrompt = @"
Here is the chat history:
```
    {{$HISTORY}}
```

The user asked this question ```{{$QUESTION}}``` and the following context was provided (if valid context was found) ```{{$CONTEXT}}```
        
Use this informatino to respond to the user as best as possible. If you are not very sure about something, say that you do not know. You respond:";

        PromptTemplateConfig promptConfig = new()
        {
            Completion =
            {
                MaxTokens = 2000,
                Temperature = 0.4,
                TopP = 0.65
            }
        };

        PromptTemplate promptTemplate = new(skPrompt, promptConfig, kernel);
        SemanticFunctionConfig functionConfig = new(promptConfig, promptTemplate);

        // Register the semantic function itself, params: (plugin name, function name, function config)
        return kernel.RegisterSemanticFunction("AskPdfQuestion", "AskAway", functionConfig);
    }

    private async Task<string> GetChatResponse(IKernel kernel, ISKFunction chatFunction, string userQuestion,
        string memoryCollectionName, string sessionId)
    {
        SKContext kernelContext = kernel.CreateNewContext();

        string? fileContext = null;
        // IEnumerable<Chunk> chunks =
        //     await _textEmbeddingService.GetChunks("matthew_dev", "622e1e17-e1e1-4a15-8b37-a57073e12052");
        // foreach (Chunk chunk in chunks)
        //     await kernel.Memory.SaveInformationAsync(memoryCollectionName, chunk.Text, chunk.GetHashCode().ToString(),
        //         chunk.SourceFile);
        await foreach (MemoryQueryResult memory in kernel.Memory.SearchAsync(memoryCollectionName, userQuestion, 5, 0.5))
            fileContext = fileContext + Environment.NewLine + memory.Metadata.Text;

        string history = string.Empty;

        _chatHistoryService.AddUserMessage(sessionId, userQuestion);

        kernelContext.Variables["HISTORY"] = _chatHistoryService.GetChatHistory(sessionId);
        kernelContext.Variables["CONTEXT"] = fileContext ?? "[no context found]";
        kernelContext.Variables["QUESTION"] = userQuestion;

        var result = (await chatFunction.InvokeAsync(kernelContext)).Result;

        _chatHistoryService.AddAgentMessage(sessionId, result);

        return result;
    }
}