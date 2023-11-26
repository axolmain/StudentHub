using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace StudentHub.Server;

public class AiChatObjects
{
    public AiChatObjects(IKernel iKernel, ISemanticTextMemory semanticTextMemory)
    {
        Kernel = iKernel;
        SemanticTextMemory = semanticTextMemory;
    }
    
    public IKernel Kernel { get; }
    public ISemanticTextMemory SemanticTextMemory { get; set; }
}