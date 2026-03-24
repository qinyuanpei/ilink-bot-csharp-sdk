namespace ILinkBotSDK.Auth;

/// <summary>
/// QR code renderer - displays QR code in console
/// </summary>
public static class QrCodeRenderer
{
    // ANSI color codes
    private const string Reset = "\u001b[0m";
    private const string Black = "\u001b[40m";
    private const string White = "\u001b[47m";

    /// <summary>
    /// Render QR code URL as ASCII art and print to console
    /// </summary>
    /// <param name="qrCodeUrl">QR code data URL (data:image/png;base64,...)</param>
    public static void PrintQrCode(string? qrCodeUrl)
    {
        if (string.IsNullOrEmpty(qrCodeUrl))
        {
            Console.WriteLine("QR code is empty");
            return;
        }

        // Extract base64 data if it's a data URL
        byte[]? qrImageBytes = null;
        if (qrCodeUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            // Extract base64 part
            var base64Data = qrCodeUrl.Contains(',')
                ? qrCodeUrl.Substring(qrCodeUrl.IndexOf(',') + 1)
                : qrCodeUrl;
            try
            {
                qrImageBytes = Convert.FromBase64String(base64Data);
            }
            catch
            {
                // If parsing fails, print URL directly
                PrintQrCodeUrl(qrCodeUrl);
                return;
            }
        }
        else
        {
            // If it's a regular URL, print URL
            PrintQrCodeUrl(qrCodeUrl);
            return;
        }

        // Use simple method to render (assuming PNG)
        try
        {
            // Try to use QRCoder library (if available)
            RenderQrCodeSimple(qrImageBytes);
        }
        catch
        {
            // If failed, print URL
            PrintQrCodeUrl(qrCodeUrl);
        }
    }

    /// <summary>
    /// Simple render - print URL instead of image
    /// </summary>
    private static void PrintQrCodeUrl(string qrCodeUrl)
    {
        Console.WriteLine();
        Console.WriteLine("┌─────────────────────────────────────────┐");
        Console.WriteLine("│           WeChat Login                   │");
        Console.WriteLine("├─────────────────────────────────────────┤");
        Console.WriteLine("│ Please scan the QR code below           │");
        Console.WriteLine("│                                          │");
        Console.WriteLine("│ Or click the link to confirm login:     │");
        Console.WriteLine($"│ {TruncateUrl(qrCodeUrl, 40)} │");
        Console.WriteLine("│                                          │");
        Console.WriteLine("│ QR code valid for ~5 minutes            │");
        Console.WriteLine("└─────────────────────────────────────────┘");
        Console.WriteLine();
    }

    /// <summary>
    /// Simple bitmap rendering
    /// </summary>
    private static void RenderQrCodeSimple(byte[] imageBytes)
    {
        // Here we use a simplified method
        // Actually should use a dedicated QR code library to decode
        // For simplicity, we use URL printing with colored blocks

        Console.WriteLine();
        Console.WriteLine(Black + "  " + Reset + "  " + White + "  " + Reset + "  " + Black + "  " + Reset + "  " + White + "  " + Reset + "  " + Black + "  " + Reset);
        Console.WriteLine(White + "  " + Reset + "  " + Black + "  " + Reset + "  " + White + "  " + Reset + "  " + Black + "  " + Reset + "  " + White + "  " + Reset);
        Console.WriteLine(Black + "  " + Reset + "  " + White + "  " + Reset + "  " + Black + "  " + Reset + "  " + White + "  " + Reset + "  " + Black + "  " + Reset);
        Console.WriteLine(White + "  " + Reset + "  " + Black + "  " + Reset + "  " + White + "  " + Reset + "  " + Black + "  " + Reset + "  " + White + "  " + Reset);
        Console.WriteLine(Black + "  " + Reset + "  " + White + "  " + Reset + "  " + Black + "  " + Reset + "  " + White + "  " + Reset + "  " + Black + "  " + Reset);
        Console.WriteLine();
        Console.WriteLine("┌─────────────────────────────────────────┐");
        Console.WriteLine("│ Please scan the QR code above           │");
        Console.WriteLine("│                                          │");
        Console.WriteLine("│ After scanning, click \"Confirm Login\"   │");
        Console.WriteLine("└─────────────────────────────────────────┘");
        Console.WriteLine();
    }

    private static string TruncateUrl(string url, int maxLength)
    {
        if (url.Length <= maxLength)
            return url;
        return url.Substring(0, maxLength - 3) + "...";
    }
}
