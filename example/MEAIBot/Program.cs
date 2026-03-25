using ILinkBotSDK;
using ILinkBotSDK.DependencyInjection;
using ILinkBotSDK.Models;
using MEAIBot;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MEAIChatMessage = Microsoft.Extensions.AI.ChatMessage;
using MEAIChatRole = Microsoft.Extensions.AI.ChatRole;

Console.WriteLine("=== MEAIBot Demo ===");
Console.WriteLine();

// 加载配置
var config = AppConfig.Load();
Console.WriteLine($"Provider: {config.Provider}");
var modelId = config.Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) ? config.OpenAI_Model : config.Anthropic_Model;
Console.WriteLine($"Model: {modelId}");
Console.WriteLine();

// Build host with AI and Bot services
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        // Add chat client based on config
        services.AddSingleton<IChatClient>(ChatClientFactory.Create(config));

        // Add ILinkBot
        services.AddILinkBot(options =>
        {
            options.BaseUrl = "https://ilinkai.weixin.qq.com";
            options.StateDirectory = "./.ilink";
        });
    })
    .Build();

// Get services
var bot = host.Services.GetRequiredService<ILinkBot>();
var chatClient = host.Services.GetRequiredService<IChatClient>();
var chatOptions = new ChatOptions() { ModelId = modelId };

// Store conversation history per user
var conversationHistory = new Dictionary<string, List<MEAIChatMessage>>();

try
{
    // Login
    Console.WriteLine("Logging in...");
    await bot.LoginAsync(true);

    Console.WriteLine($"Logged in! BotId: {bot.BotId}, UserId: {bot.UserId}");
    Console.WriteLine();

    Console.WriteLine("Starting message loop (Press Ctrl+C to exit)...");
    Console.WriteLine();

    while (bot.IsConnected)
    {
        try
        {
            var messages = await bot.RecvAsync(TimeSpan.FromSeconds(35));

            foreach (var msg in messages)
            {
                var item = msg.ItemList?.FirstOrDefault();
                if (item == null) continue;

                // Extract message text
                var text = ExtractMessageText(item);
                if (string.IsNullOrEmpty(text)) continue;

                var userId = msg.FromUserId!;
                Console.WriteLine($"Received from {userId}: {text}");

                // Initialize conversation history for user if needed
                if (!conversationHistory.ContainsKey(userId))
                {
                    conversationHistory[userId] = new List<MEAIChatMessage>
                    {
                        new(MEAIChatRole.System, "You are a helpful AI assistant. Reply concisely. Your name is WeChat ClawBot")
                    };
                }

                // Add user message to history
                conversationHistory[userId].Add(new MEAIChatMessage(MEAIChatRole.User, text));

                // Send typing indicator
                await bot.SendTypingAsync(userId);

                try
                {
                    // Get AI response using GetResponseAsync
                    var response = await chatClient.GetResponseAsync(conversationHistory[userId], chatOptions);

                    var reply = response.Messages.FirstOrDefault()?.Text ?? "Sorry, I couldn't generate a response.";
                    Console.WriteLine($"AI Response: {reply}");

                    // Add AI response to history
                    conversationHistory[userId].Add(new MEAIChatMessage(MEAIChatRole.Assistant, reply));

                    // Send reply
                    var success = await bot.SendAsync(userId, reply);
                    Console.WriteLine($"Send result: {(success ? "Success" : "Failed")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AI Error: {ex.Message}");
                    await bot.SendAsync(userId, "Sorry, I'm having trouble responding right now.");
                }

                Console.WriteLine();

                // Clear typing indicator
                await bot.StopTypingAsync(userId);
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout is normal, continue polling
            continue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Receive error: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    await bot.CloseAsync();
    await host.StopAsync();
}

/// <summary>
/// Extract text from message item
/// </summary>
static string? ExtractMessageText(MessageItem item)
{
    if (item.TextItem != null)
    {
        return item.TextItem.Text;
    }
    else if (item.VoiceItem != null)
    {
        return item.VoiceItem.Text ?? "[Voice message]";
    }
    else if (item.ImageItem != null)
    {
        return "[Image]";
    }
    else if (item.FileItem != null)
    {
        return $"[File: {item.FileItem.FileName}]";
    }
    else if (item.VideoItem != null)
    {
        return "[Video]";
    }

    return null;
}
