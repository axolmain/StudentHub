using Microsoft.SemanticKernel;

namespace Backend.Services;

public class MemoryService
{
    private readonly IKernel _kernel;

    public MemoryService(IKernel kernel)
    {
        _kernel = kernel;
    }

    public async Task SaveInformationAsync(string type, string info, string id)
    {
        await _kernel.Memory.SaveInformationAsync(type, info, id);
    }
}