namespace ILinkBotSDK;

/// <summary>
/// QR code login status
/// </summary>
public enum LoginStatus
{
    /// <summary>
    /// Waiting for user to scan QR code
    /// </summary>
    Waiting,

    /// <summary>
    /// User has scanned QR code
    /// </summary>
    Scanned,

    /// <summary>
    /// User has confirmed login
    /// </summary>
    Confirmed,

    /// <summary>
    /// QR code has expired
    /// </summary>
    Expired
}

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

    /// <summary>
    /// Enable console output (QR code display, status messages)
    /// </summary>
    public bool EnableConsoleOutput { get; set; } = true;
}