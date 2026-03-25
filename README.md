# ILinkBotSDK

A lightweight .NET SDK for WeChat iLink Bot protocol.

## Features

- QR Code Login
- Message Receiving (Long Polling)
- Message Sending
- Typing Status
- State Persistence (SQLite)
- Dependency Injection Support

## Installation

```bash
dotnet add package ILinkBotSDK
```

## Quick Start

```csharp
using ILinkBotSDK;

// Create bot instance
var bot = new ILinkBot(new ILinkBotOptions
{
    BaseUrl = "https://ilinkai.weixin.qq.com",
    StateDirectory = "./.ilink"
});

// Login (QR code login, will reuse saved credentials automatically)
await bot.LoginAsync();

Console.WriteLine($"Logged in! BotId: {bot.BotId}");

// Receive messages
while (bot.IsConnected)
{
    var messages = await bot.RecvAsync(TimeSpan.FromSeconds(35));

    foreach (var msg in messages)
    {
        var text = msg.ItemList?.FirstOrDefault()?.TextItem?.Text;
        if (string.IsNullOrEmpty(text)) continue;

        Console.WriteLine($"Received: {text}");

        // Reply
        await bot.SendTextAsync(msg.FromUserId!, "Echo: " + text);
    }
}

await bot.CloseAsync();
```

## Dependency Injection

```csharp
var services = new ServiceCollection();
services.AddILinkBot(options =>
{
    options.BaseUrl = "https://ilinkai.weixin.qq.com";
    options.StateDirectory = "./.ilink";
});

var provider = services.BuildServiceProvider();
var bot = provider.GetRequiredService<ILinkBot>();

await bot.LoginAsync();

// ... use bot

await bot.CloseAsync();
```

## API Reference

| Method/Property | Description |
|-----------------|-------------|
| `LoginAsync(force=false)` | Login via QR code, auto-reuse credentials if available |
| `RecvAsync(timeout=35s)` | Long poll for messages |
| `SendTextAsync(to, text)` | Send text message |
| `SendFileAsync(to, filePath)` | Send file from local path |
| `SendRemoteFileAsync(to, url)` | Send file from URL |
| `SendTypingAsync(to)` | Send typing status |
| `StopTypingAsync(to)` | Cancel typing status |
| `CloseAsync()` | Save state and cleanup |
| `IsConnected` | Connection status |
| `BotId` | Bot ID |
| `UserId` | Logged in user ID |

## Project Structure

```
src/ILinkBotSDK/
├── Models/          # Data models
├── Api/             # HTTP client
├── Auth/            # Login service
├── Messaging/       # Message send/receive
├── Storage/         # SQLite persistence
└── ILinkBot.cs      # Main class
```

## Examples

### EchoBot
A simple echo bot that replies to messages with a prefix.

```bash
cd example/EchoBot
dotnet run
```

Features:
- Auto-reply with "[Bot]" prefix
- Supports text, image, video, file, and voice messages

### MEAIBot
An AI-powered bot that integrates with OpenAI or Anthropic AI. Supports multi-turn conversations.

```bash
cd example/MEAIBot

# Copy and configure the environment file
cp .env.example .env
# Edit .env with your API keys

dotnet run
```

Features:
- Integration with OpenAI or Anthropic (via Microsoft.Extensions.AI)
- Multi-turn conversation history per user
- Typing indicator support

## Acknowledgments

This project is based on the [openclaw-weixin](https://github.com/hao-ji-xing/openclaw-weixin) project, which provides the protocol implementation reference for WeChat iLink Bot.

## License

MIT
