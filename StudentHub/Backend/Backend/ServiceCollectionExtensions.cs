using Backend.Services;
using Backend.Services.AiServices;

namespace Backend;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<KernelService>();
        services.AddMemoryCache();
        services.AddSingleton<EmbeddingCacheService>();
        services.AddSingleton<ChatAiService>();
        services.AddSingleton<FlashcardService>();
        services.AddSingleton<MultipleChoiceService>();
        return services;
    }
}