using ILinkBotSDK;

Console.WriteLine("=== EchoBot Demo ===");
Console.WriteLine();

// Create bot instance
var bot = new ILinkBot(new ILinkBotOptions
{
    BaseUrl = "https://ilinkai.weixin.qq.com",
    StateDirectory = "./.ilink",
    EnableConsoleOutput = true
});

bot.OnLoginStateChanged += (state, qrcode, hint) =>
{
    switch (state)
    {
        case LoginStatus.Waiting:
            Console.WriteLine(qrcode);
            Console.WriteLine($"[{state.ToString()}] {hint}");
            break;
        case LoginStatus.Scanned:
        case LoginStatus.Confirmed:
        case LoginStatus.Expired:
            Console.WriteLine($"[{state.ToString()}] {hint}");
            break;
    }
};

try
{
    // Login (will auto-reuse saved credentials)
    Console.WriteLine("Logging in...");
    await bot.LoginAsync();

    Console.WriteLine($"登录成功! 欢迎你, BotID: {bot.BotId}, UserID: {bot.UserId}");
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
                    text = $"已接收文件: {fileItem.FileName}";
                }
                else if (item.VoiceItem != null)
                {
                    text = item.VoiceItem.Text;
                }
                if (string.IsNullOrEmpty(text)) continue;

                Console.WriteLine($"[Recv] from: {msg.FromUserId} {text}");
                Console.WriteLine();

                // 对方正在输入...开始
                await bot.SendTypingAsync(msg.FromUserId!);

                // 回复文本消息
                var reply = $"[Bot] {text}";
                await bot.SendTextAsync(msg.FromUserId!, reply);

                Console.WriteLine($"[Send] from: {bot.BotId} {reply}");
                Console.WriteLine();

                // 回复文件、图片、URL
                await bot.SendFileAsync(msg.FromUserId!, Path.Combine(AppContext.BaseDirectory, "Assets/MCP_vs_Function_Calling.jpg"));
                Console.WriteLine($"[Send] from: {bot.BotId} 已发送文件 MCP_vs_Function_Calling.jpg");
                Console.WriteLine();

                await bot.SendFileAsync(msg.FromUserId!, Path.Combine(AppContext.BaseDirectory, "Assets/蜀道难.txt"));
                Console.WriteLine($"[Send] from: {bot.BotId} 已发送文件 蜀道难.txt");
                Console.WriteLine();

                await bot.SendRemoteFileAsync(msg.FromUserId!, "https://raw.githubusercontent.com/HKUDS/nanobot/b5302b6f3da12e39caad98e9a82fce47880d5c77/nanobot/channels/weixin.py");
                Console.WriteLine($"[Send] from: {bot.BotId} 已发送文件 weixin.py");
                Console.WriteLine();

                // 对方正在输入...结束
                await bot.StopTypingAsync(msg.FromUserId!);
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
