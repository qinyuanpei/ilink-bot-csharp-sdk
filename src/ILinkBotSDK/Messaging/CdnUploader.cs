using System.Security.Cryptography;
using System.Text;
using ILinkBotSDK.Api;
using ILinkBotSDK.Models;

namespace ILinkBotSDK.Messaging;

/// <summary>
/// CDN file uploader
/// </summary>
public class CdnUploader
{
    private const string CdnBaseUrl = "https://novac2c.cdn.weixin.qq.com/c2c";

    private readonly WeixinApiClient _apiClient;

    public CdnUploader(WeixinApiClient apiClient)
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
            FileMd5 = rawMd5,
            FileName = Path.GetFileName(filePath),
            MediaType = mediaType
        };
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

        string downloadParam = response.Headers.TryGetValues("X-Encrypted-Param", out var vals)
            ? vals.First()
            : throw new InvalidOperationException("CDN upload: missing X-Encrypted-Param header");

        return downloadParam;
    }

    private static byte[] EncryptAesEcb(byte[] plaintext, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(plaintext);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }
}