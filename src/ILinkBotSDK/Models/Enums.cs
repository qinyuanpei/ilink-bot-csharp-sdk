namespace ILinkBotSDK.Models;

/// <summary>
/// Message type
/// </summary>
public static class MessageType
{
    public const int None = 0;
    public const int User = 1;      // Message from user
    public const int Bot = 2;       // Message from bot
}

/// <summary>
/// Message content type
/// </summary>
public static class MessageItemType
{
    public const int None = 0;
    public const int Text = 1;      // Text
    public const int Image = 2;    // Image
    public const int Voice = 3;    // Voice
    public const int File = 4;     // File
    public const int Video = 5;    // Video
}

/// <summary>
/// Message state
/// </summary>
public static class MessageState
{
    public const int New = 0;
    public const int Generating = 1;
    public const int Finish = 2;
}

/// <summary>
/// Media type for CDN upload
/// </summary>
public static class UploadMediaType
{
    public const int Image = 1;
    public const int Video = 2;
    public const int File = 3;
    public const int Voice = 4;
}

/// <summary>
/// Typing status
/// </summary>
public static class TypingStatus
{
    public const int Typing = 1;
    public const int Cancel = 2;
}
