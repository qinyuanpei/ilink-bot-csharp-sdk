namespace ILinkBotSDK.Storage;

/// <summary>
/// State storage interface
/// </summary>
public interface IStateStorage
{
    /// <summary>
    /// Get bot state
    /// </summary>
    Task<BotStateData?> GetBotStateAsync();

    /// <summary>
    /// Save bot state
    /// </summary>
    Task SaveBotStateAsync(BotStateData state);

    /// <summary>
    /// Delete bot state
    /// </summary>
    Task DeleteBotStateAsync();

    /// <summary>
    /// Get context token
    /// </summary>
    Task<string?> GetContextTokenAsync(string userId);

    /// <summary>
    /// Save context token
    /// </summary>
    Task SaveContextTokenAsync(string userId, string contextToken);

    /// <summary>
    /// Delete context token
    /// </summary>
    Task DeleteContextTokenAsync(string userId);
}
