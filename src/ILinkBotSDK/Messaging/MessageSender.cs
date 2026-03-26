using ILinkBotSDK.Api;
using ILinkBotSDK.Models;
using ILinkBotSDK.Storage;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ILinkBotSDK.Messaging;

/// <summary>
/// Message sender
/// </summary>
public class MessageSender : IMessageSender
{
    private readonly IWeixinApiClient _apiClient;
    private readonly IStateStorage _stateStorage;
    private readonly ILogger<MessageSender>? _logger;
    private readonly Dictionary<string, string> _typingTickets = new();

    public MessageSender(
        IWeixinApiClient apiClient,
        IStateStorage stateStorage,
        ILogger<MessageSender>? logger = null)
    {
        _apiClient = apiClient;
        _stateStorage = stateStorage;
        _logger = logger;
    }

    /// <summary>
    /// Send text message
    /// </summary>
    /// <param name="to">Recipient user ID</param>
    /// <param name="text">Text content</param>
    /// <param name="contextToken">Context token (obtained from received message)</param>
    public async Task SendTextAsync(string to, string text, string? contextToken = null)
    {
        // If no contextToken provided, try to get from storage
        if (string.IsNullOrEmpty(contextToken))
        {
            contextToken = await _stateStorage.GetContextTokenAsync(to);
        }

        if (string.IsNullOrEmpty(contextToken))
        {
            throw new ApiException(-1, $"No context token available for user {to}");
        }

        var message = new WeixinMessage
        {
            FromUserId = string.Empty,
            ToUserId = to,
            ClientId = Guid.NewGuid().ToString(),
            MessageType = MessageType.Bot,
            MessageState = MessageState.Finish,
            ContextToken = contextToken,
            ItemList = new List<MessageItem>
            {
                new MessageItem
                {
                    Type = MessageItemType.Text,
                    TextItem = new TextItem { Text = text }
                }
            }
        };

        var response = await _apiClient.SendMessageAsync(message);
        if (!response.IsSuccess)
        {
            throw new ApiException(response.Ret, response.ErrMsg ?? "Failed to send message");
        }
    }

    /// <summary>
    /// Get or refresh typing ticket
    /// </summary>
    private async Task<string?> GetTypingTicketAsync(string userId, string? contextToken = null)
    {
        // Try to get from cache
        if (_typingTickets.TryGetValue(userId, out var cachedTicket))
        {
            return cachedTicket;
        }

        try
        {
            // Get contextToken from storage
            if (string.IsNullOrEmpty(contextToken))
            {
                contextToken = await _stateStorage.GetContextTokenAsync(userId);
            }

            var configResponse = await _apiClient.GetConfigAsync(userId, contextToken);
            if (configResponse.IsSuccess && !string.IsNullOrEmpty(configResponse.TypingTicket))
            {
                _typingTickets[userId] = configResponse.TypingTicket;
                return configResponse.TypingTicket;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get typing ticket for {UserId}", userId);
        }

        return null;
    }

    /// <summary>
    /// Send "typing" status
    /// </summary>
    public async Task SendTypingAsync(string to)
    {
        await SendTypingStatusAsync(to, TypingStatus.Typing);
    }

    /// <summary>
    /// Cancel "typing" status
    /// </summary>
    public async Task StopTypingAsync(string to)
    {
        await SendTypingStatusAsync(to, TypingStatus.Cancel);
    }

    private async Task SendTypingStatusAsync(string userId, int status)
    {
        try
        {
            var ticket = await GetTypingTicketAsync(userId);
            if (string.IsNullOrEmpty(ticket))
            {
                _logger?.LogWarning("No typing ticket available for {UserId}", userId);
                return;
            }

            await _apiClient.SendTypingAsync(userId, ticket, status);
            _logger?.LogDebug("Typing status sent to {UserId}: {Status}", userId, status);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send typing status to {UserId}", userId);
        }
    }

    /// <summary>
    /// Save context token
    /// </summary>
    public async Task SaveContextTokenAsync(string userId, string contextToken)
    {
        await _stateStorage.SaveContextTokenAsync(userId, contextToken);
    }

    /// <summary>
    /// Send file message
    /// </summary>
    public async Task SendFileAsync(string to, UploadedFile uploaded)
    {
        var contextToken = await _stateStorage.GetContextTokenAsync(to);
        if (string.IsNullOrEmpty(contextToken))
        {
            throw new ApiException(-1, $"No context token available for user {to}");
        }

        var cdnMedia = new CdnMedia
        {
            EncryptQueryParam = uploaded.DownloadParam,
            AesKey = HexToBase64(uploaded.AesKeyHex!),
            EncryptType = 1
        };

        var messageItem = uploaded.MediaType switch
        {
            UploadMediaType.Image => new MessageItem
            {
                Type = MessageItemType.Image,
                ImageItem = new ImageItem { Media = cdnMedia, MidSize = uploaded.CipherSize }
            },
            UploadMediaType.Video => new MessageItem
            {
                Type = MessageItemType.Video,
                VideoItem = new VideoItem { Media = cdnMedia, VideoSize = uploaded.CipherSize }
            },
            UploadMediaType.Voice => new MessageItem
            {
                Type = MessageItemType.Voice,
                VoiceItem = new VoiceItem { Media = cdnMedia }
            },
            _ => new MessageItem
            {
                Type = MessageItemType.File,
                FileItem = new FileItem
                {
                    Media = cdnMedia,
                    FileName = uploaded.FileName,
                    Md5 = uploaded.FileMd5,
                    Len = uploaded.FileSize.ToString()
                }
            }
        };

        var message = new WeixinMessage
        {
            FromUserId = string.Empty,
            ToUserId = to,
            ClientId = Guid.NewGuid().ToString(),
            MessageType = MessageType.Bot,
            MessageState = MessageState.Finish,
            ContextToken = contextToken,
            ItemList = new List<MessageItem> { messageItem }
        };

        var response = await _apiClient.SendMessageAsync(message);
        if (!response.IsSuccess)
        {
            throw new ApiException(response.Ret, response.ErrMsg ?? "Failed to send file");
        }
    }

    private string HexToBase64(string hex)
    {
        // Python: base64.b64encode(aes_key_hex.encode()).decode()
        // So we encode the hex string as bytes, then base64 encode
        byte[] bytes = Encoding.UTF8.GetBytes(hex);
        return Convert.ToBase64String(bytes);
    }
}
