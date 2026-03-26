using ILinkBotSDK.Models;

namespace ILinkBotSDK.Api;

/// <summary>
/// WeChat API client interface
/// </summary>
public interface IWeixinApiClient : IDisposable
{
    /// <summary>
    /// Set base URL
    /// </summary>
    void SetBaseUrl(string baseUrl);

    /// <summary>
    /// Set CDN base URL
    /// </summary>
    void SetCdnBaseUrl(string cdnBaseUrl);

    /// <summary>
    /// Get base URL
    /// </summary>
    string? GetBaseUrl();

    /// <summary>
    /// Get CDN base URL
    /// </summary>
    string? GetCdnBaseUrl();

    /// <summary>
    /// Set authentication token
    /// </summary>
    void SetToken(string token);

    /// <summary>
    /// Get QR code for login
    /// </summary>
    Task<GetBotQrcodeResponse> GetBotQrcodeAsync(string botType = "3");

    /// <summary>
    /// Get QR code status
    /// </summary>
    Task<GetQrcodeStatusResponse> GetQrcodeStatusAsync(string qrcode);

    /// <summary>
    /// Get updates/messages
    /// </summary>
    Task<GetUpdatesResponse> GetUpdatesAsync(string? getUpdatesBuf, int timeoutMs = 35000);

    /// <summary>
    /// Send message
    /// </summary>
    Task<SendMessageResponse> SendMessageAsync(WeixinMessage message);

    /// <summary>
    /// Get config for typing status
    /// </summary>
    Task<GetConfigResponse> GetConfigAsync(string ilinkUserId, string? contextToken = null);

    /// <summary>
    /// Send typing status
    /// </summary>
    Task<SendTypingResponse> SendTypingAsync(string ilinkUserId, string typingTicket, int status);

    /// <summary>
    /// Get upload URL for CDN
    /// </summary>
    Task<GetUploadUrlResponse> GetUploadUrlAsync(GetUploadUrlRequest request);
}