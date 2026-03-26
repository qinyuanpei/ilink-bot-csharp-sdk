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

        // Register HttpClient with proper handler configuration
        services.AddHttpClient("ILinkBot")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            });

        // Register API client (singleton, reuses HttpClient)
        services.AddSingleton<IWeixinApiClient>(sp =>
        {
            var logger = sp.GetService<ILogger<WeixinApiClient>>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ILinkBot");
            var client = new WeixinApiClient(httpClient, logger);
            client.SetBaseUrl(options.BaseUrl);
            return client;
        });

        // Register login service
        services.AddSingleton<IQrCodeLoginService>(sp =>
        {
            var logger = sp.GetService<ILogger<QrCodeLoginService>>();
            return new QrCodeLoginService(
                sp.GetRequiredService<IWeixinApiClient>(),
                logger,
                options.EnableConsoleOutput);
        });

        // Register message services
        services.AddSingleton<IMessageReceiver>(sp =>
        {
            var logger = sp.GetService<ILogger<MessageReceiver>>();
            return new MessageReceiver(
                sp.GetRequiredService<IWeixinApiClient>(),
                sp.GetRequiredService<IStateStorage>(),
                logger);
        });

        services.AddSingleton<IMessageSender>(sp =>
        {
            var logger = sp.GetService<ILogger<MessageSender>>();
            return new MessageSender(
                sp.GetRequiredService<IWeixinApiClient>(),
                sp.GetRequiredService<IStateStorage>(),
                logger);
        });

        services.AddSingleton<ICdnHelper>(sp =>
        {
            return new CdnHelper(sp.GetRequiredService<IWeixinApiClient>());
        });

        // Register bot instance
        services.AddSingleton<ILinkBot>(sp =>
        {
            var logger = sp.GetService<ILogger<ILinkBot>>();
            return new ILinkBot(
                sp.GetRequiredService<IWeixinApiClient>(),
                sp.GetRequiredService<IStateStorage>(),
                sp.GetRequiredService<IQrCodeLoginService>(),
                sp.GetRequiredService<IMessageReceiver>(),
                sp.GetRequiredService<IMessageSender>(),
                sp.GetRequiredService<ICdnHelper>(),
                options,
                logger);
        });

        return services;
    }
}