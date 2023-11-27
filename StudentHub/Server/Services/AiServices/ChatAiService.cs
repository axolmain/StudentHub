using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace StudentHub.Server.Services.AiServices;

public class ChatAiService
{
    private static TextEmbeddingService? textEmbeddingService;
    private readonly ChatHistoryService chatHistoryService;
    private readonly EmbeddingCacheService embeddingCacheService;
    private readonly KernelService kernelService;
    private AiChatObjects? _aiChatObjects;
    private readonly IHubContext<EmbeddingsHub> hubContext;


    public ChatAiService(EmbeddingCacheService embeddingCacheService,
        ChatHistoryService chatHistoryService, KernelService kernelService, 
        TextEmbeddingService? textEmbeddingService, IHubContext<EmbeddingsHub> hubContext)
    {
        this.embeddingCacheService = embeddingCacheService;
        this.chatHistoryService = chatHistoryService;
        this.kernelService = kernelService;
        this.hubContext = hubContext;

        ChatAiService.textEmbeddingService = textEmbeddingService;
    }

    public async Task<string> Execute(string userQuestion, string studySessionId, string userId)
    {
        _aiChatObjects = await kernelService.GetKernel(userId, studySessionId);
        string memoryCollectionName = $"TestingForUser{userId}WithStudySessionId{studySessionId}";
        await RefreshMemory(_aiChatObjects.SemanticTextMemory, userId, studySessionId, memoryCollectionName);
        string responses = await GetChatResponse(_aiChatObjects, userQuestion, memoryCollectionName, studySessionId);

        return responses;
    }
    
    public async Task<bool> DoesKernelExist(string studySessionId)
    {
        return await kernelService.CheckKernelExists(studySessionId);
    }
    
    public async Task GenerateEmbeddingsAsync(string studySessionId, string userId)
    {
        _aiChatObjects = await kernelService.GetKernel(userId, studySessionId);
        string memoryCollectionName = $"TestingForUser{userId}WithStudySessionId{studySessionId}";
        await RefreshMemory(_aiChatObjects.SemanticTextMemory, userId, studySessionId, memoryCollectionName);
        await hubContext.Clients.All.SendAsync("EmbeddingsGenerated");
    }

    private async Task RefreshMemory(ISemanticTextMemory? textMemory, string? userId, string studySessionId,
        string memoryCollectionName)
    {
        if (userId != null && embeddingCacheService.TryGetEmbeddings(studySessionId, out var cachedEmbeddings))
        {
            _aiChatObjects.SemanticTextMemory = cachedEmbeddings;
            return;
        }

        var chunks = await textEmbeddingService.GetChunks(userId, studySessionId);
        foreach (var chunk in chunks)
            await textMemory.SaveInformationAsync(
                memoryCollectionName, 
                chunk.Text,
                chunk.GetHashCode().ToString(),
                chunk.SourceFile
                );
        embeddingCacheService.SetEmbeddings(studySessionId, textMemory);
        _aiChatObjects.SemanticTextMemory = textMemory;
    }

    private async Task<string> GetChatResponse(AiChatObjects? aiChatObject, string userQuestion, string memoryCollectionName,
        string sessionId)
    {
        string? fileContext = null;
        string? memories = await kernelService.RetrieveMemoryAsync(aiChatObject.SemanticTextMemory, memoryCollectionName, userQuestion);
        fileContext = fileContext + Environment.NewLine + memories;

        string history = string.Empty;

        chatHistoryService.AddUserMessage(sessionId, userQuestion);

        string result = await kernelService.AskAboutDocumentsAsync(aiChatObject.Kernel, chatHistoryService.GetChatHistory(sessionId),
            userQuestion, fileContext);

        chatHistoryService.AddAgentMessage(sessionId, result);

        return result;
    }
}