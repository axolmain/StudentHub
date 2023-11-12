using Backend.Services;
using Backend.Services.AiServices;
using Backend.Services.DataService;

namespace Backend;

public static class ServiceCollectionExtensions2
{
    public static IServiceCollection AddServices2(this IServiceCollection services)
    {
        return
            services
                .AddSingleton<ChatHistoryService>()
                .AddSingleton<IDataService, DataService>()
                .AddSingleton<TextEmbeddingService>()
                .AddSingleton<MaterialCacheService>()
                .AddSingleton<IUserAuthService, UserAuthService>();
    }
}