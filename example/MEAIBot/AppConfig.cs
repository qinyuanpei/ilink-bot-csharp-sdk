using DotNetEnv;

namespace MEAIBot;

public class AppConfig
{
    public string Provider { get; set; } = "OpenAI";
    public string OpenAI_ApiKey { get; set; } = "";
    public string OpenAI_Model { get; set; } = "gpt-4o-2024-11-20";
    public string OpenAI_BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Anthropic_ApiKey { get; set; } = "";
    public string Anthropic_Model { get; set; } = "claude-sonnet-4-20250514";
    public string Anthropic_BaseUrl { get; set; } = "https://api.anthropic.com";

    public static AppConfig Load()
    {
        var provider = Environment.GetEnvironmentVariable("PROVIDER") ?? "OpenAI";
        var openAI_ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        var openAI_Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-2024-11-20";
        var openAI_BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
        var anthropic_ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "";
        var anthropic_Model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-sonnet-4-20250514";
        var anthropic_BaseUrl = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? "https://api.anthropic.com";

        Env.Load();
        provider = Environment.GetEnvironmentVariable("PROVIDER") ?? provider;
        openAI_ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? openAI_ApiKey;
        openAI_Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? openAI_Model;
        openAI_BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? openAI_BaseUrl;
        anthropic_ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? anthropic_ApiKey;
        anthropic_Model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? anthropic_Model;
        anthropic_BaseUrl = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? anthropic_BaseUrl;

        return new AppConfig
        {
            Provider = provider,
            OpenAI_ApiKey = openAI_ApiKey,
            OpenAI_Model = openAI_Model,
            OpenAI_BaseUrl = openAI_BaseUrl,
            Anthropic_ApiKey = anthropic_ApiKey,
            Anthropic_Model = anthropic_Model,
            Anthropic_BaseUrl = anthropic_BaseUrl
        };
    }
}
