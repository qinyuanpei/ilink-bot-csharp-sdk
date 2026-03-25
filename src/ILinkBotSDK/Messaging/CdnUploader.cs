using System.Security.Cryptography;
using ILinkBotSDK.Api;
using ILinkBotSDK.Models;

namespace ILinkBotSDK.Messaging;

/// <summary>
/// CDN file uploader
/// </summary>
public class CdnUploader
{
    private readonly WeixinApiClient _apiClient;

    public CdnUploader(WeixinApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Upload file to CDN
    /// </summary>
    /// <param name="fileStream">File stream</param>
    /// <param name="fileName">File name</param>
    /// <param name="mediaType">Media type</param>
    /// <param name="toUserId">Recipient user ID</param>
    /// <returns>Uploaded file info with download parameters</returns>
    public async Task<UploadedFile?> UploadAsync(Stream fileStream, string fileName, int mediaType, string? toUserId = null)
    {
        // Calculate file size and MD5
        fileStream.Position = 0;
        var fileSize = (int)fileStream.Length;
        var md5 = await CalculateMd5Async(fileStream);

        fileStream.Position = 0;

        // Step 1: Get upload URL
        var uploadUrlRequest = new GetUploadUrlRequest
        {
            FileKey = Guid.NewGuid().ToString("N"),
            MediaType = mediaType,
            ToUserId = toUserId,
            RawSize = fileSize,
            RawFileMd5 = md5,
            FileSize = fileSize,
            NoNeedThumb = true
        };

        var uploadUrlResponse = await _apiClient.GetUploadUrlAsync(uploadUrlRequest);
        if (!uploadUrlResponse.IsSuccess || string.IsNullOrEmpty(uploadUrlResponse.UploadParam))
        {
            return null;
        }

        // Step 2: Upload to CDN
        var uploadSuccess = await UploadToCdnAsync(fileStream, uploadUrlResponse.UploadParam);
        if (!uploadSuccess)
        {
            return null;
        }

        // Step 3: Return uploaded file info
        return new UploadedFile
        {
            FileKey = uploadUrlRequest.FileKey,
            FileSize = fileSize,
            FileMd5 = md5,
            FileName = fileName,
            MediaType = mediaType
        };
    }

    private async Task<bool> UploadToCdnAsync(Stream fileStream, string uploadParam)
    {
        try
        {
            // Decode upload param to get upload URL
            var uploadUrl = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(uploadParam));

            fileStream.Position = 0;

            using var httpClient = new HttpClient();
            using var content = new StreamContent(fileStream);

            // Set content headers
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await httpClient.PostAsync(uploadUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> CalculateMd5Async(Stream stream)
    {
        using var md5 = MD5.Create();
        var hash = await md5.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Uploaded file info
/// </summary>
public class UploadedFile
{
    /// <summary>
    /// File key returned from upload
    /// </summary>
    public string? FileKey { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public int FileSize { get; set; }

    /// <summary>
    /// File MD5 hash
    /// </summary>
    public string? FileMd5 { get; set; }

    /// <summary>
    /// Original file name
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Media type
    /// </summary>
    public int MediaType { get; set; }
}