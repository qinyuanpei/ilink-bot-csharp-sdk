using Microsoft.Extensions.Logging;
using ILinkBotSDK.Api;
using ILinkBotSDK.Models;
using ILinkBotSDK.Storage;

namespace ILinkBotSDK.Messaging;

/// <summary>
/// Message receiver (long polling)
/// </summary>
public class MessageReceiver : IMessageReceiver
{
    private readonly IWeixinApiClient _apiClient;
    private readonly IStateStorage _stateStorage;
    private readonly ILogger<MessageReceiver>? _logger;
    private string? _currentCursor;

    public MessageReceiver(
        IWeixinApiClient apiClient,
        IStateStorage stateStorage,
        ILogger<MessageReceiver>? logger = null)
    {
        _apiClient = apiClient;
        _stateStorage = stateStorage;
        _logger = logger;
    }

    /// <summary>
    /// Set cursor (restore from persistence)
    /// </summary>
    public void SetCursor(string? cursor)
    {
        _currentCursor = cursor;
    }

    /// <summary>
    /// Long poll to receive messages
    /// </summary>
    /// <param name="timeout">Poll timeout, recommended 35 seconds</param>
    /// <returns>List of received messages</returns>
    public async Task<List<WeixinMessage>> ReceiveAsync(TimeSpan timeout)
    {
        try
        {
            var timeoutMs = (int)timeout.TotalMilliseconds;
            var response = await _apiClient.GetUpdatesAsync(_currentCursor, timeoutMs);

            // Update cursor
            if (!string.IsNullOrEmpty(response.GetUpdatesBuf))
            {
                _currentCursor = response.GetUpdatesBuf;

                // Save to persistence
                var state = await _stateStorage.GetBotStateAsync();
                if (state != null)
                {
                    state.GetUpdatesBuf = _currentCursor;
                    await _stateStorage.SaveBotStateAsync(state);
                }
            }

            // Filter messages
            var messages = response.Msgs ?? new List<WeixinMessage>();
            var userMessages = messages
                .Where(m => m.MessageType == MessageType.User && m.MessageState == MessageState.Finish)
                .ToList();

            if (userMessages.Count > 0)
            {
                _logger?.LogInformation("Received {Count} user messages", userMessages.Count);
            }

            return userMessages;
        }
        catch (OperationCanceledException)
        {
            // Timeout is normal, return empty list
            _logger?.LogDebug("Long polling timeout, returning empty messages");
            return new List<WeixinMessage>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error receiving messages");
            throw;
        }
    }
}
