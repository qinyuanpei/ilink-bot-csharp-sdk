using ILinkBotSDK.Models;

namespace ILinkBotSDK.Messaging;

/// <summary>
/// CDN helper interface
/// </summary>
public interface ICdnHelper
{
    /// <summary>
    /// Upload file to CDN
    /// </summary>
    Task<UploadedFile> UploadFileAsync(string toUserId, string filePath, CancellationToken ct = default);

    /// <summary>
    /// Upload file from URL to CDN
    /// </summary>
    Task<UploadedFile> UploadFromUrlAsync(string toUserId, string url, CancellationToken ct = default);

    /// <summary>
    /// Download file from CDN
    /// </summary>
    Task DownloadAsync(CdnMedia media, string filePath, CancellationToken ct = default);
}