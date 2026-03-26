using ILinkBotSDK;

Console.WriteLine("=== EchoBot Demo ===");
Console.WriteLine();

// Create bot instance
var bot = new ILinkBot(new ILinkBotOptions
{
    BaseUrl = "https://ilinkai.weixin.qq.com",
    StateDirectory = "./.ilink"
});

try
{
    // Login (will auto-reuse saved credentials)
    Console.WriteLine("Logging in...");
    await bot.LoginAsync(true);

    Console.WriteLine($"登录成功! BotID: {bot.BotId}, UserID: {bot.UserId}");
    Console.WriteLine();

    // 接收消息循环
    Console.WriteLine("开始接收消息（按 Ctrl+C 退出）...");
    Console.WriteLine();

    while (bot.IsConnected)
    {
        try
        {
            var messages = await bot.RecvAsync(TimeSpan.FromSeconds(35));

            foreach (var msg in messages)
            {
                // 获取消息内容
                var item = msg.ItemList?.FirstOrDefault();
                if (item == null) continue;

                var text = string.Empty;
                if (item.TextItem != null)
                {
                    text = item.TextItem.Text;
                }
                else if (item.ImageItem != null)
                {
                    text = item.ImageItem.Url;
                }
                else if (item.VideoItem?.Media != null)
                {
                    text = item.VideoItem.Media.EncryptQueryParam ?? string.Empty;
                }
                else if (item.FileItem != null)
                {
                    var fileItem = item.FileItem;
                    var filePath = Path.Combine(AppContext.BaseDirectory, fileItem.FileName!);
                    await bot.File.DownloadAsync(fileItem.Media!, filePath);
                    text = $"已接收文件{fileItem.FileName}";
                }
                else if (item.VoiceItem != null)
                {
                    text = item.VoiceItem.Text;
                }
                if (string.IsNullOrEmpty(text)) continue;

                Console.WriteLine($"收到消息 from: {msg.FromUserId}");
                Console.WriteLine($"内容: {text}");
                Console.WriteLine();

                // 自动回复
                var reply = $"[Bot] {text}";
                Console.WriteLine($"发送回复: {reply}");

                await bot.SendTextAsync(msg.FromUserId!, reply);
                Console.WriteLine();

                await bot.SendFileAsync(msg.FromUserId!, "D:\\Documents\\MCP-Training\\MCP_For_.NET_Developers.pptx");
                await bot.SendFileAsync(msg.FromUserId!, "D:\\Documents\\MCP-Training\\MCP_vs_Function_Calling.jpg");
                await bot.SendRemoteFileAsync(msg.FromUserId!, "https://raw.githubusercontent.com/HKUDS/nanobot/b5302b6f3da12e39caad98e9a82fce47880d5c77/nanobot/channels/weixin.py");
            }
        }
        catch (OperationCanceledException)
        {
            // 超时是正常的，继续轮询
            continue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"接收消息错误: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"错误: {ex.Message}");
}
finally
{
    await bot.CloseAsync();
}
