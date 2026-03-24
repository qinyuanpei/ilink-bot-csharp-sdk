namespace ILinkBotSDK.Storage;

/// <summary>
/// Bot state data
/// </summary>
public class BotStateData
{
    /// <summary>
    /// Bot ID (xxx@im.bot)
    /// </summary>
    public string? BotId { get; set; }

    /// <summary>
    /// User ID (xxx@im.wechat)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Bot Token
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// API base URL
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// CDN base URL
    /// </summary>
    public string? CdnBaseUrl { get; set; }

    /// <summary>
    /// GetUpdates cursor
    /// </summary>
    public string? GetUpdatesBuf { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Update time
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Context token cache
/// </summary>
public class ContextTokenData
{
    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Context Token
    /// </summary>
    public string? ContextToken { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Update time
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
