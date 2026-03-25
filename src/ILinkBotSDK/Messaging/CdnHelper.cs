using ILinkBotSDK.Api;
using ILinkBotSDK.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace ILinkBotSDK.Messaging;

/// <summary>
/// CDN helper for uploading and downloading files
/// </summary>
public class CdnHelper
{
    private const string CdnBaseUrl = "https://novac2c.cdn.weixin.qq.com/c2c";

    private readonly WeixinApiClient _apiClient;

    public CdnHelper(WeixinApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Upload file to CDN
    /// </summary>
    /// <param name="toUserId">Recipient user ID</param>
    /// <param name="filePath">Local file path</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Uploaded file info with download parameters</returns>
    public async Task<UploadedFile> UploadAsync(
        string toUserId,
        string filePath,
        CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        // Read file bytes
        var data = await File.ReadAllBytesAsync(filePath, ct);

        // Determine media type based on file extension
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var mediaType = GetMediaType(ext);

        // Generate random fileKey and AES key (16 bytes each)
        byte[] fileKeyBytes = RandomNumberGenerator.GetBytes(16);
        byte[] aesKeyBytes = RandomNumberGenerator.GetBytes(16);

        string fileKeyHex = Convert.ToHexString(fileKeyBytes).ToLowerInvariant();
        string aesKeyHex = Convert.ToHexString(aesKeyBytes).ToLowerInvariant();

        // MD5 of plaintext
        string rawMd5 = Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant();

        // AES-128-ECB with PKCS7 padding
        byte[] encrypted = EncryptAesEcb(data, aesKeyBytes);

        var uploadReq = new GetUploadUrlRequest
        {
            FileKey = fileKeyHex,
            MediaType = mediaType,
            ToUserId = toUserId,
            RawSize = data.Length,
            RawFileMd5 = rawMd5,
            FileSize = encrypted.Length,
            NoNeedThumb = true,
            AesKey = aesKeyHex,
            BaseInfo = new BaseInfo()
        };

        var uploadResp = await _apiClient.GetUploadUrlAsync(uploadReq);
        if (uploadResp.Ret != 0)
            throw new InvalidOperationException(
                $"GetUploadUrl failed: ret={uploadResp.Ret} errmsg={uploadResp.ErrMsg}");

        string downloadParam = await UploadToCdnAsync(encrypted, uploadResp.UploadParam!, fileKeyHex, ct);

        return new UploadedFile
        {
            DownloadParam = downloadParam,
            AesKeyHex = aesKeyHex,
            FileSize = data.Length,
            CipherSize = encrypted.Length,
            FileName = Path.GetFileName(filePath),
            MediaType = mediaType,
            FileMd5 = rawMd5
        };
    }

    /// <summary>
    /// Upload file from URL to CDN
    /// </summary>
    /// <param name="toUserId">Recipient user ID</param>
    /// <param name="url">Source URL to download from</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Uploaded file info with download parameters</returns>
    public async Task<UploadedFile> UploadFromUrlAsync(
        string toUserId,
        string url,
        CancellationToken ct = default)
    {
        // Download file to temp location
        var tempPath = await DownloadToTempAsync(url, ct);

        try
        {
            // Upload the downloaded file
            var uploaded = await UploadAsync(toUserId, tempPath, ct);
            return uploaded;
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private async Task<string> DownloadToTempAsync(string url, CancellationToken ct)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        // Try to get filename from Content-Disposition header
        var fileName = GetFileNameFromHeaders(response.Headers, response.RequestMessage?.RequestUri);

        // Create temp file
        var tempDir = Path.Combine(Path.GetTempPath(), "ILinkBotSDK");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, fileName);

        // Download content
        using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await contentStream.CopyToAsync(fileStream, ct);

        return tempPath;
    }

    private static string GetFileNameFromHeaders(HttpHeaders headers, Uri? requestUri)
    {
        // Try Content-Disposition header
        if (headers.TryGetValues("Content-Disposition", out var cdValues))
        {
            foreach (var cd in cdValues)
            {
                var match = System.Text.RegularExpressions.Regex.Match(cd, @"filename\*?=""?([^"";]+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }

        // Fall back to URL path
        if (requestUri != null && !string.IsNullOrEmpty(requestUri.AbsolutePath))
        {
            var fileName = Path.GetFileName(requestUri.AbsolutePath);
            if (!string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }
        }

        // Default filename
        return $"download_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

    /// <summary>
    /// Download file from CDN
    /// </summary>
    /// <param name="media">CDN media info from received message</param>
    /// <param name="filePath">Local path to save the file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Whether download was successful</returns>
    public async Task<bool> DownloadAsync(CdnMedia media, string filePath, CancellationToken ct = default)
    {
        if (media == null || string.IsNullOrEmpty(media.EncryptQueryParam))
        {
            return false;
        }

        return await DownloadAsync(media.EncryptQueryParam, media.AesKey, filePath, ct);
    }

    /// <summary>
    /// Download file from CDN
    /// </summary>
    /// <param name="encryptQueryParam">Encrypted query parameter from CDN</param>
    /// <param name="aesKey">AES key (base64 encoded or hex string)</param>
    /// <param name="filePath">Local path to save the file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Whether download was successful</returns>
    private async Task<bool> DownloadAsync(string encryptQueryParam, string? aesKey, string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(encryptQueryParam))
        {
            return false;
        }

        try
        {
            // Build CDN download URL
            var url = $"{CdnBaseUrl}/download" +
                      $"?encrypted_query_param={Uri.EscapeDataString(encryptQueryParam)}";

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var response = await httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var data = await response.Content.ReadAsByteArrayAsync(ct);

            // Decrypt if AES key is provided
            if (!string.IsNullOrEmpty(aesKey))
            {
                data = DecryptAesEcb(data, aesKey);
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(filePath, data, ct);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private static int GetMediaType(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => UploadMediaType.Image,
            ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" => UploadMediaType.Video,
            ".mp3" or ".wav" or ".aac" or ".ogg" or ".m4a" => UploadMediaType.Voice,
            _ => UploadMediaType.File
        };
    }

    /// <summary>
    /// Convert AES key hex to Base64
    /// </summary>
    public static string AesKeyToBase64(string hexKey) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(hexKey));

    private static async Task<string> UploadToCdnAsync(
        byte[] encrypted, string uploadParam, string fileKey, CancellationToken ct)
    {
        string url = $"{CdnBaseUrl}/upload" +
                     $"?encrypted_query_param={Uri.EscapeDataString(uploadParam)}" +
                     $"&filekey={Uri.EscapeDataString(fileKey)}";

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        using var content = new ByteArrayContent(encrypted);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        using var response = await httpClient.PostAsync(url, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"CDN upload HTTP {(int)response.StatusCode}: {body}");
        }

        string downloadParam = response.Headers.TryGetValues("x-encrypted-param", out var vals)
            ? vals.First()
            : throw new InvalidOperationException("CDN upload: missing x-encrypted-param header");

        return downloadParam;
    }

    private static byte[] EncryptAesEcb(byte[] plaintext, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    private static byte[] DecryptAesEcb(byte[] ciphertext, string aesKey)
    {
        // Parse AES key: base64 decode first, then check if it's raw or hex-encoded
        // - If decoded is 16 bytes -> raw key
        // - If decoded is 32 bytes hex string -> convert hex to 16 bytes
        byte[] decoded = Convert.FromBase64String(aesKey);

        byte[] key;
        if (decoded.Length == 16)
        {
            key = decoded;
        }
        else if (decoded.Length == 32 && IsHexString(Encoding.ASCII.GetString(decoded)))
        {
            // Hex-encoded key: convert hex string to raw bytes
            key = Convert.FromHexString(decoded);
        }
        else
        {
            throw new ArgumentException($"Invalid AES key length: {decoded.Length}");
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    private static bool IsHexString(string s)
    {
        foreach (char c in s)
        {
            if (!char.IsAsciiHexDigit(c))
                return false;
        }
        return true;
    }
}