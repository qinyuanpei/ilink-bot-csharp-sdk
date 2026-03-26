using ILinkBotSDK.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ILinkBotSDK.Api;

/// <summary>
/// iLink API client
/// </summary>
public class WeixinApiClient : IWeixinApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeixinApiClient>? _logger;
    private string? _token;
    private string _baseUrl = "https://ilinkai.weixin.qq.com";
    private string _cdnBaseUrl = "https://novac2c.cdn.weixin.qq.com/c2c";
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public WeixinApiClient(HttpClient? httpClient = null, ILogger<WeixinApiClient>? logger = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
    }

    public void SetBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _logger?.LogDebug("Base URL set to: {BaseUrl}", _baseUrl);
    }

    public void SetCdnBaseUrl(string cdnBaseUrl)
    {
        _cdnBaseUrl = cdnBaseUrl.TrimEnd('/');
    }

    public void SetToken(string? token)
    {
        _token = token;
    }

    public string? GetToken() => _token;
    public string? GetBaseUrl() => _baseUrl;
    public string? GetCdnBaseUrl() => _cdnBaseUrl;

    /// <summary>
    /// Generate X-WECHAT-UIN header: random uint32 -> decimal string -> Base64
    /// </summary>
    private static string GenerateWechatUin()
    {
        var randomBytes = new byte[4];
        Random.Shared.NextBytes(randomBytes);
        var uint32 = BitConverter.ToUInt32(randomBytes, 0);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(uint32.ToString()));
    }

    /// <summary>
    /// Set HTTP request headers
    /// </summary>
    private void SetRequestHeader(HttpRequestMessage httpRequestMessage)
    {
        var headers = new Dictionary<string, string>
        {
            ["AuthorizationType"] = "ilink_bot_token",
            ["X-WECHAT-UIN"] = GenerateWechatUin()
        };

        if (!string.IsNullOrEmpty(_token))
        {
            headers["Authorization"] = $"Bearer {_token}";
        }

        foreach (var header in headers)
        {
            httpRequestMessage.Headers.Add(header.Key, header.Value);
        }
    }

    /// <summary>
    /// POST JSON request
    /// </summary>
    private async Task<T> PostJsonAsync<T>(string endpoint, object? body, int timeoutMs = 15000) where T : ApiResponse
    {
        var url = $"{_baseUrl}/{endpoint.TrimStart('/')}";
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = content;

        SetRequestHeader(request);

        using var cts = new CancellationTokenSource(timeoutMs);
        var response = await _httpClient.SendAsync(request, cts.Token);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, responseText);
        }

        var result = JsonSerializer.Deserialize<T>(responseText, JsonOptions);
        if (result == null)
        {
            throw new ApiException("Failed to deserialize response");
        }

        if (!result.IsSuccess)
        {
            throw new ApiException(result.ErrCode ?? result.Ret, result.ErrMsg);
        }

        return result;
    }

    /// <summary>
    /// GET request
    /// </summary>
    private async Task<T> GetAsync<T>(string endpoint, int timeoutMs = 15000) where T : class
    {
        var url = $"{_baseUrl}/{endpoint.TrimStart('/')}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        using var cts = new CancellationTokenSource(timeoutMs);
        var response = await _httpClient.SendAsync(request, cts.Token);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, responseText);
        }

        var result = JsonSerializer.Deserialize<T>(responseText, JsonOptions);
        if (result == null)
        {
            throw new ApiException("Failed to deserialize response");
        }

        return result;
    }

    /// <summary>
    /// Get login QR code
    /// </summary>
    public async Task<GetBotQrcodeResponse> GetBotQrcodeAsync(string botType = "3")
    {
        return await GetAsync<GetBotQrcodeResponse>($"/ilink/bot/get_bot_qrcode?bot_type={botType}");
    }

    /// <summary>
    /// Poll QR code status
    /// </summary>
    public async Task<GetQrcodeStatusResponse> GetQrcodeStatusAsync(string qrcode)
    {
        return await GetAsync<GetQrcodeStatusResponse>($"/ilink/bot/get_qrcode_status?qrcode={Uri.EscapeDataString(qrcode)}");
    }

    /// <summary>
    /// Long poll for messages
    /// </summary>
    public async Task<GetUpdatesResponse> GetUpdatesAsync(string? getUpdatesBuf, int timeoutMs = 35000)
    {
        var request = new GetUpdatesRequest
        {
            GetUpdatesBuf = getUpdatesBuf ?? "",
            BaseInfo = new BaseInfo { ChannelVersion = "1.0.0" }
        };

        return await PostJsonAsync<GetUpdatesResponse>("ilink/bot/getupdates", request, timeoutMs);
    }

    /// <summary>
    /// Send message
    /// </summary>
    public async Task<SendMessageResponse> SendMessageAsync(WeixinMessage message)
    {
        var request = new SendMessageRequest
        {
            Msg = message,
            BaseInfo = new BaseInfo { ChannelVersion = "1.0.0" }
        };

        return await PostJsonAsync<SendMessageResponse>("ilink/bot/sendmessage", request);
    }

    /// <summary>
    /// Get config (includes typing_ticket)
    /// </summary>
    public async Task<GetConfigResponse> GetConfigAsync(string ilinkUserId, string? contextToken = null)
    {
        var request = new GetConfigRequest
        {
            IlinkUserId = ilinkUserId,
            ContextToken = contextToken,
            BaseInfo = new BaseInfo { ChannelVersion = "1.0.0" }
        };

        return await PostJsonAsync<GetConfigResponse>("ilink/bot/getconfig", request);
    }

    /// <summary>
    /// Send typing status
    /// </summary>
    public async Task<SendTypingResponse> SendTypingAsync(string ilinkUserId, string typingTicket, int status)
    {
        var request = new SendTypingRequest
        {
            IlinkUserId = ilinkUserId,
            TypingTicket = typingTicket,
            Status = status,
            BaseInfo = new BaseInfo { ChannelVersion = "1.0.0" }
        };

        return await PostJsonAsync<SendTypingResponse>("ilink/bot/sendtyping", request);
    }

    /// <summary>
    /// Get CDN upload URL
    /// </summary>
    public async Task<GetUploadUrlResponse> GetUploadUrlAsync(GetUploadUrlRequest request)
    {
        request.BaseInfo = new BaseInfo { ChannelVersion = "1.0.0" };
        return await PostJsonAsync<GetUploadUrlResponse>("ilink/bot/getuploadurl", request);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
