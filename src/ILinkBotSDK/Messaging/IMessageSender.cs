using ILinkBotSDK.Models;

namespace ILinkBotSDK.Messaging;

/// <summary>
/// Message sender interface
/// </summary>
public interface IMessageSender
{
    /// <summary>
    /// Send text message
    /// </summary>
    Task SendTextAsync(string to, string text, string? contextToken = null);

    /// <summary>
    /// Send typing indicator
    /// </summary>
    Task SendTypingAsync(string to);

    /// <summary>
    /// Stop typing indicator
    /// </summary>
    Task StopTypingAsync(string to);

    /// <summary>
    /// Save context token
    /// </summary>
    Task SaveContextTokenAsync(string userId, string contextToken);

    /// <summary>
    /// Send file message
    /// </summary>
    Task SendFileAsync(string to, UploadedFile uploaded);
}