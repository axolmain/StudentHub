using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;

namespace StudentHub.Server.Services.AiServices;

public class ChatAiService
{
    private readonly EmbeddingCacheService embeddingCacheService;
    private readonly ChatHistoryService chatHistoryService;
    private readonly KernelService kernelService;
    private static TextEmbeddingService? textEmbeddingService;
    private IKernel? _kernel;

    public ChatAiService(EmbeddingCacheService embeddingCacheService, 
        ChatHistoryService chatHistoryService,
        KernelService kernelService, TextEmbeddingService? textEmbeddingService)
    {
        this.embeddingCacheService = embeddingCacheService;
        this.chatHistoryService = chatHistoryService;
        this.kernelService = kernelService;
        ChatAiService.textEmbeddingService = textEmbeddingService;
    }

    public async Task<string> Execute(string userQuestion, string studySessionId, string userId)
    {
        _kernel = await kernelService.GetKernel(userId, studySessionId);
        string memoryCollectionName = $"Testing/{userId}/{studySessionId}";
        await RefreshMemory(_kernel, userId, studySessionId, memoryCollectionName);
        string responses = await GetChatResponse(_kernel, userQuestion, memoryCollectionName, studySessionId);

        return responses;
    }

    private async Task RefreshMemory(IKernel? kernel, string? userId, string studySessionId,
        string memoryCollectionName)
    {
        if (userId != null && embeddingCacheService.TryGetEmbeddings(studySessionId, out ISemanticTextMemory cachedEmbeddings ))
        {
            kernel?.RegisterMemory(cachedEmbeddings);
            return;
        }
        IEnumerable<Chunk> chunks = await textEmbeddingService.GetChunks(userId, studySessionId);
        foreach (Chunk chunk in chunks)
            await kernelService.SaveMemoryAsync(kernel, memoryCollectionName, chunk.Text, chunk.GetHashCode().ToString(),
                chunk.SourceFile);
        embeddingCacheService.SetEmbeddings(studySessionId, kernel.Memory);
    }
    
    private async Task<string> GetChatResponse(IKernel? kernel, string userQuestion, string memoryCollectionName, string sessionId)
    {
        string? fileContext = null;
        string? memories = await kernelService.RetrieveMemoryAsync(kernel, memoryCollectionName, userQuestion);
            fileContext = fileContext + Environment.NewLine + memories;

        string history = string.Empty;

        chatHistoryService.AddUserMessage(sessionId, userQuestion);

        string result = await kernelService.AskAboutDocumentsAsync(kernel, chatHistoryService.GetChatHistory(sessionId), fileContext, userQuestion);

        chatHistoryService.AddAgentMessage(sessionId, result);

        return result;
    }
}