using Anthropic.SDK;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace MEAIBot;

public static class ChatClientFactory
{
    public static IChatClient Create(AppConfig config)
    {
        if (config.Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return CreateOpenAIClient(config);
        }

        if (config.Provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return CreateAnthropicClient(config);
        }

        throw new NotSupportedException($"Provider {config.Provider} not supported");
    }

    private static IChatClient CreateOpenAIClient(AppConfig config)
    {
        var openAIClient = new OpenAI.Chat.ChatClient(
            model: config.OpenAI_Model,
            new ApiKeyCredential(config.OpenAI_ApiKey),
            new OpenAIClientOptions() { Endpoint = new Uri(config.OpenAI_BaseUrl) }
        );

        return openAIClient.AsIChatClient();
    }

    private static IChatClient CreateAnthropicClient(AppConfig config)
    {
        var handler = new AnthropicMessageRedirectHandler(config.Anthropic_BaseUrl);
        var httpClient = new HttpClient(handler);

        var anthropicClient = new AnthropicClient(client: httpClient);
        anthropicClient.Auth = new APIAuthentication(apiKey: config.Anthropic_ApiKey);

        return anthropicClient.Messages
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}

/// <summary>
/// 用于 Anthropic 的消息端点重定向 Handler
/// </summary>
public class AnthropicMessageRedirectHandler : HttpClientHandler
{
    private const string AnthropicMessagePath = "/v1/messages";
    private readonly string _customBaseUrl;

    public AnthropicMessageRedirectHandler(string customBaseUrl)
    {
        _customBaseUrl = customBaseUrl;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && request.RequestUri.AbsolutePath.EndsWith(AnthropicMessagePath))
        {
            var newUri = new Uri($"{_customBaseUrl.TrimEnd('/')}{AnthropicMessagePath}");
            request.RequestUri = newUri;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
