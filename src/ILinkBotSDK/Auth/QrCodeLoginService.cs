using Microsoft.Extensions.Logging;
using ILinkBotSDK.Api;
using QRCoder;

namespace ILinkBotSDK.Auth;

/// <summary>
/// QR code login service
/// </summary>
public class QrCodeLoginService
{
    private readonly WeixinApiClient _apiClient;
    private readonly ILogger<QrCodeLoginService>? _logger;
    private string? _currentQrCode;
    private string? _currentQrCodeId;
    private string? _currentSessionKey;
    private DateTime _qrCodeStartTime;
    private const int QrPollTimeoutMs = 35000;    // 35 seconds poll timeout
    private const int MaxQrRefreshCount = 10;      // Allow more refreshes

    public QrCodeLoginService(WeixinApiClient apiClient, ILogger<QrCodeLoginService>? logger = null)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Start QR code login
    /// </summary>
    public async Task<QrCodeStartResult> StartLoginAsync(string? existingSessionKey = null, bool force = false)
    {
        // Check if we have an existing login session (if not forcing refresh)
        if (!force && !string.IsNullOrEmpty(_currentQrCode))
        {
            // Display existing QR code
            PrintQRCode(_currentQrCode);
            Console.WriteLine("QR code is ready, please scan with WeChat");
            Console.WriteLine();

            return new QrCodeStartResult
            {
                QrCodeUrl = _currentQrCode,
                SessionKey = _currentSessionKey,
                Message = "QR code is ready, please scan with WeChat"
            };
        }

        try
        {
            // Get QR code
            var qrResponse = await _apiClient.GetBotQrcodeAsync();

            _currentQrCode = qrResponse.QrcodeImgContent;
            _currentQrCodeId = qrResponse.Qrcode;
            _currentSessionKey = existingSessionKey ?? Guid.NewGuid().ToString();
            _qrCodeStartTime = DateTime.UtcNow;

            _logger?.LogInformation("QR code retrieved, session key: {SessionKey}", _currentSessionKey);

            // Display QR code in console
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("                    WeChat iLink Login                      ");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine();

            PrintQRCode(qrResponse.QrcodeImgContent ?? string.Empty);

            Console.WriteLine("Please scan the QR code above with WeChat");
            Console.WriteLine("After scanning, please click \"Confirm Login\"");
            Console.WriteLine();
            Console.WriteLine("Tip: If the QR code image cannot be displayed, please copy the link below and open in browser:");
            Console.WriteLine($"  {qrResponse.QrcodeImgContent}");
            Console.WriteLine();

            return new QrCodeStartResult
            {
                QrCodeUrl = qrResponse.QrcodeImgContent,
                SessionKey = _currentSessionKey,
                Message = "Please scan the QR code with WeChat"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get QR code");
            return new QrCodeStartResult
            {
                Message = $"Failed to get QR code: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Wait for QR code scan confirmation
    /// </summary>
    public async Task<LoginResult> WaitForConfirmationAsync(string sessionKey, int timeoutMs = 480000)
    {
        if (string.IsNullOrEmpty(_currentQrCode))
        {
            return new LoginResult
            {
                Success = false,
                Message = "No login in progress, please start login first"
            };
        }

        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        int refreshCount = 0;
        bool scannedPrinted = false;
        int pollCount = 0;

        Console.WriteLine("Waiting for scan... (Press Ctrl+C to cancel)");
        Console.WriteLine();

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // Poll status using the QR code ID returned from server
                var qrCodeToPoll = _currentQrCodeId ?? _currentQrCode;
                if (string.IsNullOrEmpty(qrCodeToPoll))
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "QR code information lost"
                    };
                }

                var statusResponse = await _apiClient.GetQrcodeStatusAsync(qrCodeToPoll);
                pollCount++;

                switch (statusResponse.Status)
                {
                    case QrCodeStatus.Wait:
                        // Print waiting indicator every 10 seconds
                        if (pollCount % 10 == 0)
                        {
                            Console.Write(".");
                        }
                        break;

                    case QrCodeStatus.Scaned:
                        if (!scannedPrinted)
                        {
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine("═══════════════════════════════════════════════════");
                            Console.WriteLine("              ✅ Scanned, please confirm!          ");
                            Console.WriteLine("═══════════════════════════════════════════════════");
                            Console.WriteLine();
                            scannedPrinted = true;
                        }
                        break;

                    case QrCodeStatus.Confirmed:
                        if (string.IsNullOrEmpty(statusResponse.IlinkBotId))
                        {
                            return new LoginResult
                            {
                                Success = false,
                                Message = "Login failed: server did not return bot ID"
                            };
                        }

                        _logger?.LogInformation(
                            "Login confirmed! BotId: {BotId}, UserId: {UserId}",
                            statusResponse.IlinkBotId,
                            statusResponse.IlinkUserId);

                        Console.WriteLine();
                        Console.WriteLine("═══════════════════════════════════════════════════");
                        Console.WriteLine("              ✅ Login successful!                 ");
                        Console.WriteLine("═══════════════════════════════════════════════════");
                        Console.WriteLine();

                        // Clear current session
                        ClearSession();

                        return new LoginResult
                        {
                            Success = true,
                            BotId = statusResponse.IlinkBotId,
                            UserId = statusResponse.IlinkUserId,
                            Token = statusResponse.BotToken,
                            BaseUrl = statusResponse.BaseUrl,
                            Message = "Login successful"
                        };

                    case QrCodeStatus.Expired:
                        refreshCount++;
                        if (refreshCount > MaxQrRefreshCount)
                        {
                            _logger?.LogWarning("QR code expired {MaxRefreshCount} times, giving up", MaxQrRefreshCount);
                            ClearSession();
                            return new LoginResult
                            {
                                Success = false,
                                Message = "Login timeout: QR code expired multiple times, please restart login"
                            };
                        }

                        // Refresh QR code
                        _logger?.LogInformation("QR code expired, refreshing ({RefreshCount}/{MaxRefreshCount})",
                            refreshCount, MaxQrRefreshCount);

                        Console.WriteLine();
                        Console.WriteLine($"⚠️  QR code expired, refreshing ({refreshCount}/{MaxQrRefreshCount})...");
                        Console.WriteLine();

                        var refreshResult = await StartLoginAsync(sessionKey, force: true);
                        if (string.IsNullOrEmpty(refreshResult.QrCodeUrl))
                        {
                            ClearSession();
                            return new LoginResult
                            {
                                Success = false,
                                Message = $"Failed to refresh QR code: {refreshResult.Message}"
                            };
                        }

                        scannedPrinted = false;
                        pollCount = 0;
                        break;
                }

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error polling QR status");
                ClearSession();
                return new LoginResult
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        _logger?.LogWarning("Login timeout");
        ClearSession();
        return new LoginResult
        {
            Success = false,
            Message = "Login timeout, please try again"
        };
    }

    private void PrintQRCode(string content)
    {
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.L);
            using var code = new AsciiQRCode(data);
            string ascii = code.GetGraphic(1, drawQuietZones: true);
            Console.WriteLine(ascii);
        }
        catch
        {
            // Fallback: print the URL itself
            Console.WriteLine($"(QR rendering failed — open in browser: {content})");
        }
    }

    private void ClearSession()
    {
        _currentQrCode = null;
        _currentQrCodeId = null;
        _currentSessionKey = null;
    }
}
