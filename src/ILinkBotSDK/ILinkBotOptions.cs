namespace ILinkBotSDK;

/// <summary>
/// ILinkBot options
/// </summary>
public class ILinkBotOptions
{
    /// <summary>
    /// API base URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://ilinkai.weixin.qq.com";

    /// <summary>
    /// State storage directory
    /// </summary>
    public string StateDirectory { get; set; } = ".ilink";
}
