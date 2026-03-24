# ILinkBotSDK

轻量级 .NET 微信 iLink Bot 协议 SDK。

## 功能特性

- 扫码登录
- 消息接收（长轮询）
- 消息发送
- 正在输入状态
- 状态持久化（SQLite）
- 依赖注入支持

## 安装

```bash
dotnet add package ILinkBotSDK
```

## 快速开始

```csharp
using ILinkBotSDK;

// 创建机器人实例
var bot = new ILinkBot(new ILinkBotOptions
{
    BaseUrl = "https://ilinkai.weixin.qq.com",
    StateDirectory = "./.ilink"
});

// 登录（扫码登录，会自动复用已保存的凭证）
await bot.LoginAsync();

Console.WriteLine($"登录成功! BotId: {bot.BotId}");

// 接收消息
while (bot.IsConnected)
{
    var messages = await bot.RecvAsync(TimeSpan.FromSeconds(35));

    foreach (var msg in messages)
    {
        var text = msg.ItemList?.FirstOrDefault()?.TextItem?.Text;
        if (string.IsNullOrEmpty(text)) continue;

        Console.WriteLine($"收到消息: {text}");

        // 回复
        await bot.SendAsync(msg.FromUserId!, "回复: " + text);
    }
}

await bot.CloseAsync();
```

## 依赖注入

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

// ... 使用 bot

await bot.CloseAsync();
```

## API 参考

| 方法/属性 | 说明 |
|-----------|------|
| `LoginAsync(force=false)` | 扫码登录，已有凭证则自动复用 |
| `RecvAsync(timeout=35s)` | 长轮询接收消息 |
| `SendAsync(to, text)` | 发送文本消息 |
| `SendTypingAsync(to)` | 发送"正在输入"状态 |
| `StopTypingAsync(to)` | 取消"正在输入"状态 |
| `CloseAsync()` | 保存状态并清理 |
| `IsConnected` | 连接状态 |
| `BotId` | 机器人 ID |
| `UserId` | 登录用户 ID |

## 项目结构

```
src/ILinkBotSDK/
├── Models/          # 数据模型
├── Api/             # HTTP 客户端
├── Auth/           # 登录服务
├── Messaging/       # 消息收发
├── Storage/         # SQLite 持久化
└── ILinkBot.cs      # 主类
```

## 致谢

本项目基于 [openclaw-weixin](https://github.com/echoxu/openclaw-weixin) 项目开发，该项目提供了微信 iLink Bot 协议的完整实现参考。

## 许可证

MIT
