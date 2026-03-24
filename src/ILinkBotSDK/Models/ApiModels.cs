using System.Text.Json.Serialization;

namespace ILinkBotSDK.Models;

/// <summary>
/// Base request info
/// </summary>
public class BaseInfo
{
    [JsonPropertyName("channel_version")]
    public string? ChannelVersion { get; set; }
}

/// <summary>
/// Common API response base class
/// </summary>
public class ApiResponse
{
    [JsonPropertyName("ret")]
    public int Ret { get; set; }

    [JsonPropertyName("errcode")]
    public int? ErrCode { get; set; }

    [JsonPropertyName("errmsg")]
    public string? ErrMsg { get; set; }

    public bool IsSuccess => Ret == 0;
}

// ==================== Login Related ====================

/// <summary>
/// Get QR code response
/// </summary>
public class GetBotQrcodeResponse
{
    [JsonPropertyName("qrcode")]
    public string? Qrcode { get; set; }

    [JsonPropertyName("qrcode_img_content")]
    public string? QrcodeImgContent { get; set; }
}

/// <summary>
/// Poll QR code status response
/// </summary>
public class GetQrcodeStatusResponse : ApiResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }  // wait, scaned, confirmed, expired

    [JsonPropertyName("bot_token")]
    public string? BotToken { get; set; }

    [JsonPropertyName("ilink_bot_id")]
    public string? IlinkBotId { get; set; }

    [JsonPropertyName("baseurl")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("ilink_user_id")]
    public string? IlinkUserId { get; set; }
}

// ==================== Message Receiving Related ====================

/// <summary>
/// GetUpdates request
/// </summary>
public class GetUpdatesRequest
{
    [JsonPropertyName("sync_buf")]
    public string? SyncBuf { get; set; }

    [JsonPropertyName("get_updates_buf")]
    public string? GetUpdatesBuf { get; set; }

    [JsonPropertyName("base_info")]
    public BaseInfo? BaseInfo { get; set; }
}

/// <summary>
/// GetUpdates response
/// </summary>
public class GetUpdatesResponse : ApiResponse
{
    [JsonPropertyName("msgs")]
    public List<WeixinMessage>? Msgs { get; set; }

    [JsonPropertyName("sync_buf")]
    public string? SyncBuf { get; set; }

    [JsonPropertyName("get_updates_buf")]
    public string? GetUpdatesBuf { get; set; }

    [JsonPropertyName("longpolling_timeout_ms")]
    public int? LongPollingTimeoutMs { get; set; }
}

// ==================== Message Sending Related ====================

/// <summary>
/// SendMessage request
/// </summary>
public class SendMessageRequest
{
    [JsonPropertyName("msg")]
    public WeixinMessage? Msg { get; set; }

    [JsonPropertyName("base_info")]
    public BaseInfo? BaseInfo { get; set; }
}

/// <summary>
/// SendMessage response
/// </summary>
public class SendMessageResponse : ApiResponse
{
}

// ==================== Typing Related ====================

/// <summary>
/// GetConfig request (for getting typing_ticket)
/// </summary>
public class GetConfigRequest
{
    [JsonPropertyName("ilink_user_id")]
    public string? IlinkUserId { get; set; }

    [JsonPropertyName("context_token")]
    public string? ContextToken { get; set; }

    [JsonPropertyName("base_info")]
    public BaseInfo? BaseInfo { get; set; }
}

/// <summary>
/// GetConfig response
/// </summary>
public class GetConfigResponse : ApiResponse
{
    [JsonPropertyName("typing_ticket")]
    public string? TypingTicket { get; set; }
}

/// <summary>
/// SendTyping request
/// </summary>
public class SendTypingRequest
{
    [JsonPropertyName("ilink_user_id")]
    public string? IlinkUserId { get; set; }

    [JsonPropertyName("typing_ticket")]
    public string? TypingTicket { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }  // 1=typing, 2=cancel

    [JsonPropertyName("base_info")]
    public BaseInfo? BaseInfo { get; set; }
}

/// <summary>
/// SendTyping response
/// </summary>
public class SendTypingResponse : ApiResponse
{
}

// ==================== CDN Upload Related ====================

/// <summary>
/// GetUploadUrl request
/// </summary>
public class GetUploadUrlRequest
{
    [JsonPropertyName("filekey")]
    public string? FileKey { get; set; }

    [JsonPropertyName("media_type")]
    public int? MediaType { get; set; }

    [JsonPropertyName("to_user_id")]
    public string? ToUserId { get; set; }

    [JsonPropertyName("rawsize")]
    public int? RawSize { get; set; }

    [JsonPropertyName("rawfilemd5")]
    public string? RawFileMd5 { get; set; }

    [JsonPropertyName("filesize")]
    public int? FileSize { get; set; }

    [JsonPropertyName("thumb_rawsize")]
    public int? ThumbRawSize { get; set; }

    [JsonPropertyName("thumb_rawfilemd5")]
    public string? ThumbRawFileMd5 { get; set; }

    [JsonPropertyName("thumb_filesize")]
    public int? ThumbFileSize { get; set; }

    [JsonPropertyName("no_need_thumb")]
    public bool? NoNeedThumb { get; set; }

    [JsonPropertyName("aeskey")]
    public string? AesKey { get; set; }

    [JsonPropertyName("base_info")]
    public BaseInfo? BaseInfo { get; set; }
}

/// <summary>
/// GetUploadUrl response
/// </summary>
public class GetUploadUrlResponse : ApiResponse
{
    [JsonPropertyName("upload_param")]
    public string? UploadParam { get; set; }

    [JsonPropertyName("thumb_upload_param")]
    public string? ThumbUploadParam { get; set; }
}
