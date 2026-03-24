namespace ILinkBotSDK.Api;

/// <summary>
/// API exception
/// </summary>
public class ApiException : Exception
{
    public int ErrorCode { get; }
    public string? ErrorMessage { get; }

    public ApiException(int errorCode, string? errorMessage)
        : base($"API Error {errorCode}: {errorMessage}")
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public ApiException(string message) : base(message) { }

    public ApiException(string message, Exception innerException) : base(message, innerException) { }
}
