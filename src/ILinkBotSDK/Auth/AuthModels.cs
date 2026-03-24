namespace ILinkBotSDK.Auth;

/// <summary>
/// Login result
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public string? BotId { get; set; }
    public string? UserId { get; set; }
    public string? Token { get; set; }
    public string? BaseUrl { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// QR code login start result
/// </summary>
public class QrCodeStartResult
{
    public string? QrCodeUrl { get; set; }
    public string? SessionKey { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// QR code status
/// </summary>
public static class QrCodeStatus
{
    public const string Wait = "wait";
    public const string Scaned = "scaned";
    public const string Confirmed = "confirmed";
    public const string Expired = "expired";
}
