using System.Text.Json.Serialization;

namespace ILinkBotSDK.Models;

/// <summary>
/// WeChat message
/// </summary>
public class WeixinMessage
{
    [JsonPropertyName("seq")]
    public long? Seq { get; set; }

    [JsonPropertyName("message_id")]
    public long? MessageId { get; set; }

    [JsonPropertyName("from_user_id")]
    public string? FromUserId { get; set; }

    [JsonPropertyName("to_user_id")]
    public string? ToUserId { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("create_time_ms")]
    public long? CreateTimeMs { get; set; }

    [JsonPropertyName("update_time_ms")]
    public long? UpdateTimeMs { get; set; }

    [JsonPropertyName("delete_time_ms")]
    public long? DeleteTimeMs { get; set; }

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    [JsonPropertyName("message_type")]
    public int MessageType { get; set; }

    [JsonPropertyName("message_state")]
    public int MessageState { get; set; }

    [JsonPropertyName("item_list")]
    public List<MessageItem>? ItemList { get; set; }

    [JsonPropertyName("context_token")]
    public string? ContextToken { get; set; }
}

/// <summary>
/// Message content item
/// </summary>
public class MessageItem
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("create_time_ms")]
    public long? CreateTimeMs { get; set; }

    [JsonPropertyName("update_time_ms")]
    public long? UpdateTimeMs { get; set; }

    [JsonPropertyName("is_completed")]
    public bool? IsCompleted { get; set; }

    [JsonPropertyName("msg_id")]
    public string? MsgId { get; set; }

    [JsonPropertyName("text_item")]
    public TextItem? TextItem { get; set; }

    [JsonPropertyName("image_item")]
    public ImageItem? ImageItem { get; set; }

    [JsonPropertyName("voice_item")]
    public VoiceItem? VoiceItem { get; set; }

    [JsonPropertyName("file_item")]
    public FileItem? FileItem { get; set; }

    [JsonPropertyName("video_item")]
    public VideoItem? VideoItem { get; set; }
}

/// <summary>
/// Text message content
/// </summary>
public class TextItem
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// CDN media reference
/// </summary>
public class CdnMedia
{
    [JsonPropertyName("encrypt_query_param")]
    public string? EncryptQueryParam { get; set; }

    [JsonPropertyName("aes_key")]
    public string? AesKey { get; set; }

    [JsonPropertyName("encrypt_type")]
    public int? EncryptType { get; set; }
}

/// <summary>
/// Image message content
/// </summary>
public class ImageItem
{
    [JsonPropertyName("media")]
    public CdnMedia? Media { get; set; }

    [JsonPropertyName("thumb_media")]
    public CdnMedia? ThumbMedia { get; set; }

    [JsonPropertyName("aeskey")]
    public string? AesKey { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("mid_size")]
    public int? MidSize { get; set; }

    [JsonPropertyName("thumb_size")]
    public int? ThumbSize { get; set; }

    [JsonPropertyName("thumb_height")]
    public int? ThumbHeight { get; set; }

    [JsonPropertyName("thumb_width")]
    public int? ThumbWidth { get; set; }

    [JsonPropertyName("hd_size")]
    public int? HdSize { get; set; }
}

/// <summary>
/// Voice message content
/// </summary>
public class VoiceItem
{
    [JsonPropertyName("media")]
    public CdnMedia? Media { get; set; }

    /// <summary>
    /// Voice encoding type: 1=pcm 2=adpcm 3=feature 4=speex 5=amr 6=silk 7=mp3 8=ogg-speex
    /// </summary>
    [JsonPropertyName("encode_type")]
    public int? EncodeType { get; set; }

    [JsonPropertyName("bits_per_sample")]
    public int? BitsPerSample { get; set; }

    /// <summary>
    /// Sample rate (Hz)
    /// </summary>
    [JsonPropertyName("sample_rate")]
    public int? SampleRate { get; set; }

    /// <summary>
    /// Voice duration (milliseconds)
    /// </summary>
    [JsonPropertyName("playtime")]
    public int? Playtime { get; set; }

    /// <summary>
    /// Speech to text content
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// File message content
/// </summary>
public class FileItem
{
    [JsonPropertyName("media")]
    public CdnMedia? Media { get; set; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    [JsonPropertyName("len")]
    public string? Len { get; set; }
}

/// <summary>
/// Video message content
/// </summary>
public class VideoItem
{
    [JsonPropertyName("media")]
    public CdnMedia? Media { get; set; }

    [JsonPropertyName("video_size")]
    public int? VideoSize { get; set; }

    [JsonPropertyName("play_length")]
    public int? PlayLength { get; set; }

    [JsonPropertyName("video_md5")]
    public string? VideoMd5 { get; set; }

    [JsonPropertyName("thumb_media")]
    public CdnMedia? ThumbMedia { get; set; }

    [JsonPropertyName("thumb_size")]
    public int? ThumbSize { get; set; }

    [JsonPropertyName("thumb_height")]
    public int? ThumbHeight { get; set; }

    [JsonPropertyName("thumb_width")]
    public int? ThumbWidth { get; set; }
}
