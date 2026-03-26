using ILinkBotSDK;

namespace ILinkBotSDK.Auth;

/// <summary>
/// QR code login service interface
/// </summary>
public interface IQrCodeLoginService
{
    /// <summary>
    /// Set state changed callback
    /// </summary>
    void SetStateChangedCallback(Action<LoginStatus, string?, string?>? callback);

    /// <summary>
    /// Start QR code login
    /// </summary>
    Task<QrCodeStartResult> StartLoginAsync(string? existingSessionKey = null, bool force = false);

    /// <summary>
    /// Wait for QR code scan confirmation
    /// </summary>
    Task<LoginResult> WaitForConfirmationAsync(string sessionKey, int timeoutMs = 480000);
}