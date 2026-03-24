using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILinkBotSDK.Api;
using ILinkBotSDK.Auth;
using ILinkBotSDK.Messaging;
using ILinkBotSDK.Storage;

namespace ILinkBotSDK.DependencyInjection;

/// <summary>
/// ILinkBot dependency injection extensions
/// </summary>
public static class ILinkBotServiceCollectionExtensions
{
    /// <summary>
    /// Register ILinkBot services
    /// </summary>
    public static IServiceCollection AddILinkBot(
        this IServiceCollection services,
        Action<ILinkBotOptions>? configureOptions = null)
    {
        // Configure options
        var options = new ILinkBotOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);

        // Register storage
        services.AddSingleton<IStateStorage>(sp =>
        {
            var logger = sp.GetService<ILogger<SqliteStateStorage>>();
            var dbPath = Path.Combine(options.StateDirectory, "state.db");
            return new SqliteStateStorage(dbPath, logger);
        });

        // Register API client
        services.AddSingleton<WeixinApiClient>(sp =>
        {
            var logger = sp.GetService<ILogger<WeixinApiClient>>();
            var client = new WeixinApiClient(null, logger);
            client.SetBaseUrl(options.BaseUrl);
            return client;
        });

        // Register login service
        services.AddSingleton<QrCodeLoginService>();

        // Register message services
        services.AddSingleton<MessageReceiver>();
        services.AddSingleton<MessageSender>();

        // Register bot instance
        services.AddSingleton<ILinkBot>(sp =>
        {
            var logger = sp.GetService<ILogger<ILinkBot>>();
            return new ILinkBot(options, logger);
        });

        return services;
    }
}
