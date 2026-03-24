using Xunit;
using ILinkBotSDK.Api;
using ILinkBotSDK.Models;

namespace ILinkBotSDK.Tests;

public class ApiClientTests
{
    [Fact]
    public void CreateApiClient_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var client = new WeixinApiClient();

        // Assert
        Assert.NotNull(client);
        Assert.Null(client.GetToken());
        Assert.Equal("https://ilinkai.weixin.qq.com", client.GetBaseUrl());
    }

    [Fact]
    public void SetBaseUrl_ShouldUpdateBaseUrl()
    {
        // Arrange
        var client = new WeixinApiClient();
        var newUrl = "https://custom.ilinkai.weixin.qq.com";

        // Act
        client.SetBaseUrl(newUrl);

        // Assert
        Assert.Equal(newUrl, client.GetBaseUrl());
    }

    [Fact]
    public void SetBaseUrl_WithTrailingSlash_ShouldRemoveTrailingSlash()
    {
        // Arrange
        var client = new WeixinApiClient();
        var urlWithSlash = "https://ilinkai.weixin.qq.com/";

        // Act
        client.SetBaseUrl(urlWithSlash);

        // Assert
        Assert.Equal("https://ilinkai.weixin.qq.com", client.GetBaseUrl());
    }

    [Fact]
    public void SetToken_ShouldUpdateToken()
    {
        // Arrange
        var client = new WeixinApiClient();
        var token = "test_token_123";

        // Act
        client.SetToken(token);

        // Assert
        Assert.Equal(token, client.GetToken());
    }
}

public class MessageModelTests
{
    [Fact]
    public void WeixinMessage_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = @"
        {
            ""message_id"": 123456,
            ""from_user_id"": ""user123@im.wechat"",
            ""to_user_id"": ""bot123@im.bot"",
            ""message_type"": 1,
            ""message_state"": 2,
            ""context_token"": ""token_abc"",
            ""item_list"": [
                {
                    ""type"": 1,
                    ""text_item"": { ""text"": ""Hello"" }
                }
            ]
        }";

        // Act
        var message = System.Text.Json.JsonSerializer.Deserialize<WeixinMessage>(json);

        // Assert
        Assert.NotNull(message);
        Assert.Equal(123456, message.MessageId);
        Assert.Equal("user123@im.wechat", message.FromUserId);
        Assert.Equal("bot123@im.bot", message.ToUserId);
        Assert.Equal(MessageType.User, message.MessageType);
        Assert.Equal(MessageState.Finish, message.MessageState);
        Assert.Equal("token_abc", message.ContextToken);
        Assert.Single(message.ItemList!);
        Assert.Equal(MessageItemType.Text, message.ItemList![0].Type);
        Assert.Equal("Hello", message.ItemList[0].TextItem?.Text);
    }
}

public class EnumsTests
{
    [Fact]
    public void MessageType_ShouldHaveCorrectValues()
    {
        Assert.Equal(0, MessageType.None);
        Assert.Equal(1, MessageType.User);
        Assert.Equal(2, MessageType.Bot);
    }

    [Fact]
    public void MessageItemType_ShouldHaveCorrectValues()
    {
        Assert.Equal(0, MessageItemType.None);
        Assert.Equal(1, MessageItemType.Text);
        Assert.Equal(2, MessageItemType.Image);
        Assert.Equal(3, MessageItemType.Voice);
        Assert.Equal(4, MessageItemType.File);
        Assert.Equal(5, MessageItemType.Video);
    }

    [Fact]
    public void MessageState_ShouldHaveCorrectValues()
    {
        Assert.Equal(0, MessageState.New);
        Assert.Equal(1, MessageState.Generating);
        Assert.Equal(2, MessageState.Finish);
    }

    [Fact]
    public void TypingStatus_ShouldHaveCorrectValues()
    {
        Assert.Equal(1, TypingStatus.Typing);
        Assert.Equal(2, TypingStatus.Cancel);
    }
}
