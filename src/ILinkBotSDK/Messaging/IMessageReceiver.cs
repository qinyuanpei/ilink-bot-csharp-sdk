using ILinkBotSDK.Models;

namespace ILinkBotSDK.Messaging;

/// <summary>
/// Message receiver interface
/// </summary>
public interface IMessageReceiver
{
    /// <summary>
    /// Set cursor for long polling
    /// </summary>
    void SetCursor(string? cursor);

    /// <summary>
    /// Receive messages
    /// </summary>
    Task<List<WeixinMessage>> ReceiveAsync(TimeSpan timeout);
}