using Microsoft.Extensions.Logging;
using ILinkBotSDK.Api;
using ILinkBotSDK.Auth;
using ILinkBotSDK.Messaging;
using ILinkBotSDK.Models;
using ILinkBotSDK.Storage;

namespace ILinkBotSDK;

/// <summary>
/// iLink Bot client
/// </summary>
public class ILinkBot : IAsyncDisposable
{
    private readonly IWeixinApiClient _apiClient;
    private readonly IStateStorage _stateStorage;
    private readonly IQrCodeLoginService _loginService;
    private readonly IMessageReceiver _messageReceiver;
    private readonly IMessageSender _messageSender;
    private readonly ICdnHelper _cdnHelper;
    private readonly ILogger<ILinkBot>? _logger;
    private readonly bool _enableConsoleOutput;

    private bool _isConnected;
    private string? _botId;
    private string? _userId;

    /// <summary>
    /// Whether connected
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Bot ID
    /// </summary>
    public string? BotId => _botId;

    /// <summary>
    /// User ID (currently logged in WeChat user)
    /// </summary>
    public string? UserId => _userId;

    /// <summary>
    /// CdnHlper
    /// </summary>
    public ICdnHelper File => _cdnHelper;

    /// <summary>
    /// Called when login state changes
    /// </summary>
    public Action<LoginStatus, string?, string?>? OnLoginStateChanged;

    /// <summary>
    /// Create ILinkBot instance
    /// </summary>
    /// <param name="options">Options</param>
    /// <param name="logger">Logger</param>
    public ILinkBot(ILinkBotOptions? options = null, ILogger<ILinkBot>? logger = null)
    {
        var baseUrl = options?.BaseUrl ?? "https://ilinkai.weixin.qq.com";
        var stateDirectory = options?.StateDirectory ?? ".ilink";
        _enableConsoleOutput = options?.EnableConsoleOutput ?? true;

        // Ensure directory exists
        Directory.CreateDirectory(stateDirectory);

        // Initialize storage
        var dbPath = Path.Combine(stateDirectory, "state.db");
        _stateStorage = new SqliteStateStorage(dbPath);

        // Initialize API client
        _apiClient = new WeixinApiClient();
        _apiClient.SetBaseUrl(baseUrl);

        // Initialize services
        _loginService = new QrCodeLoginService(_apiClient, enableConsoleOutput: _enableConsoleOutput, onStateChanged: options?.OnLoginStateChanged);
        _messageReceiver = new MessageReceiver(_apiClient, _stateStorage);
        _messageSender = new MessageSender(_apiClient, _stateStorage);
        _cdnHelper = new CdnHelper(_apiClient);
        _logger = logger;
    }

    /// <summary>
    /// Create ILinkBot instance with custom services (for DI)
    /// </summary>
    public ILinkBot(
        IWeixinApiClient apiClient,
        IStateStorage stateStorage,
        IQrCodeLoginService loginService,
        IMessageReceiver messageReceiver,
        IMessageSender messageSender,
        ICdnHelper cdnHelper,
        ILinkBotOptions? options = null,
        ILogger<ILinkBot>? logger = null)
    {
        _apiClient = apiClient;
        _stateStorage = stateStorage;
        _loginService = loginService;
        _messageReceiver = messageReceiver;
        _messageSender = messageSender;
        _cdnHelper = cdnHelper;
        _logger = logger;
        _enableConsoleOutput = options?.EnableConsoleOutput ?? true;
    }

    /// <summary>
    /// Login (QR code login)
    /// </summary>
    /// <param name="force">Whether to force re-login</param>
    public async Task LoginAsync(bool force = false)
    {
        _logger?.LogInformation("Starting login, force: {Force}", force);

        // If there's saved state and not forcing login, restore directly
        if (!force)
        {
            var savedState = await _stateStorage.GetBotStateAsync();
            if (savedState != null && !string.IsNullOrEmpty(savedState.Token))
            {
                _apiClient.SetToken(savedState.Token);
                if (!string.IsNullOrEmpty(savedState.BaseUrl))
                {
                    _apiClient.SetBaseUrl(savedState.BaseUrl);
                }
                if (!string.IsNullOrEmpty(savedState.CdnBaseUrl))
                {
                    _apiClient.SetCdnBaseUrl(savedState.CdnBaseUrl);
                }

                _botId = savedState.BotId;
                _userId = savedState.UserId;
                _isConnected = true;

                // Restore cursor
                if (!string.IsNullOrEmpty(savedState.GetUpdatesBuf))
                {
                    _messageReceiver.SetCursor(savedState.GetUpdatesBuf);
                }

                _logger?.LogInformation("Restored session for bot {BotId}", _botId);
                return;
            }
        }

        // Start QR code login
        var startResult = await _loginService.StartLoginAsync(force: force);

        if (string.IsNullOrEmpty(startResult.QrCodeUrl))
        {
            throw new InvalidOperationException($"Failed to start login: {startResult.Message}");
        }

        // Wait for confirmation
        var loginResult = await _loginService.WaitForConfirmationAsync(startResult.SessionKey!);

        if (!loginResult.Success)
        {
            throw new InvalidOperationException($"Login failed: {loginResult.Message}");
        }

        // Save login info
        var state = new BotStateData
        {
            BotId = loginResult.BotId,
            UserId = loginResult.UserId,
            Token = loginResult.Token,
            BaseUrl = loginResult.BaseUrl ?? _apiClient.GetBaseUrl(),
            CdnBaseUrl = _apiClient.GetCdnBaseUrl(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _stateStorage.SaveBotStateAsync(state);

        // Set client state
        _apiClient.SetToken(loginResult.Token);
        if (!string.IsNullOrEmpty(loginResult.BaseUrl))
        {
            _apiClient.SetBaseUrl(loginResult.BaseUrl);
        }

        _botId = loginResult.BotId;
        _userId = loginResult.UserId;
        _isConnected = true;

        _logger?.LogInformation("Login successful! BotId: {BotId}, UserId: {UserId}", _botId, _userId);
    }

    /// <summary>
    /// Get login QR code (for UI display)
    /// </summary>
    public async Task<string> GetQrCodeAsync()
    {
        var result = await _loginService.StartLoginAsync();
        return result.QrCodeUrl ?? throw new InvalidOperationException(result.Message);
    }

    /// <summary>
    /// Wait for scan confirmation (used with GetQrCodeAsync)
    /// </summary>
    public async Task WaitForLoginAsync(string sessionKey, int timeoutMs = 480000)
    {
        var result = await _loginService.WaitForConfirmationAsync(sessionKey, timeoutMs);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Login failed: {result.Message}");
        }

        // Save login info
        var state = new BotStateData
        {
            BotId = result.BotId,
            UserId = result.UserId,
            Token = result.Token,
            BaseUrl = result.BaseUrl ?? _apiClient.GetBaseUrl(),
            CdnBaseUrl = _apiClient.GetCdnBaseUrl(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _stateStorage.SaveBotStateAsync(state);

        _apiClient.SetToken(result.Token);
        if (!string.IsNullOrEmpty(result.BaseUrl))
        {
            _apiClient.SetBaseUrl(result.BaseUrl);
        }

        _botId = result.BotId;
        _userId = result.UserId;
        _isConnected = true;
    }

    /// <summary>
    /// Long poll to receive messages
    /// </summary>
    /// <param name="timeout">Timeout, default 35 seconds</param>
    /// <returns>Message list</returns>
    public async Task<List<WeixinMessage>> RecvAsync(TimeSpan timeout)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected. Please call LoginAsync first.");
        }

        var messages = await _messageReceiver.ReceiveAsync(timeout);

        // Save context token for each message
        foreach (var msg in messages)
        {
            if (!string.IsNullOrEmpty(msg.FromUserId) && !string.IsNullOrEmpty(msg.ContextToken))
            {
                await _messageSender.SaveContextTokenAsync(msg.FromUserId, msg.ContextToken);
            }
        }

        return messages;
    }

    /// <summary>
    /// Send text message
    /// </summary>
    /// <param name="to">Recipient user ID</param>
    /// <param name="text">Text content</param>
    public async Task SendTextAsync(string to, string text)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected. Please call LoginAsync first.");
        }

        await _messageSender.SendTextAsync(to, text);
    }

    /// <summary>
    /// Send file message
    /// </summary>
    /// <param name="to">Recipient user ID</param>
    /// <param name="filePath">Local file path</param>
    public async Task SendFileAsync(string to, string filePath)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected. Please call LoginAsync first.");
        }

        // Upload to CDN
        var uploaded = await _cdnHelper.UploadFileAsync(to, filePath);

        // Send message with file
        await _messageSender.SendFileAsync(to, uploaded);
    }

    /// <summary>
    /// Send remote file message
    /// </summary>
    /// <param name="to">Recipient user ID</param>
    /// <param name="url">Remote file URL</param>
    public async Task SendRemoteFileAsync(string to, string url)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected. Please call LoginAsync first.");
        }

        // Upload from URL
        var uploaded = await _cdnHelper.UploadFromUrlAsync(to, url);

        // Send message with file
        await _messageSender.SendFileAsync(to, uploaded);
    }

    /// <summary>
    /// Send typing indicator
    /// </summary>
    public async Task SendTypingAsync(string to)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected. Please call LoginAsync first.");
        }

        await _messageSender.SendTypingAsync(to);
    }

    /// <summary>
    /// Cancel "typing" status
    /// </summary>
    public async Task StopTypingAsync(string to)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected. Please call LoginAsync first.");
        }

        await _messageSender.StopTypingAsync(to);
    }

    /// <summary>
    /// Close and cleanup resources
    /// </summary>
    public async Task CloseAsync()
    {
        _logger?.LogInformation("Closing bot connection");

        _isConnected = false;
        _botId = null;
        _userId = null;

        _apiClient.Dispose();

        if (_stateStorage is IDisposable disposableStorage)
        {
            disposableStorage.Dispose();
        }

        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        GC.SuppressFinalize(this);
    }
}
