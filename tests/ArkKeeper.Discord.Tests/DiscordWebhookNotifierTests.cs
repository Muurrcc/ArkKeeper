using System.Net;

namespace ArkKeeper.Discord.Tests;

public class DiscordWebhookNotifierTests
{
    [Fact]
    public async Task SendAsync_PostsToTheConfiguredWebhookUrl()
    {
        var handler = new FakeHttpMessageHandler();
        var notifier = new DiscordWebhookNotifier(new HttpClient(handler), "https://discord.com/api/webhooks/123/abc");

        await notifier.SendAsync("hello");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://discord.com/api/webhooks/123/abc", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendAsync_WithContentOnly_SerializesContentField()
    {
        var handler = new FakeHttpMessageHandler();
        var notifier = new DiscordWebhookNotifier(new HttpClient(handler), "https://discord.com/api/webhooks/123/abc");

        await notifier.SendAsync("Server restarting in 5 minutes");

        Assert.Contains("\"content\":\"Server restarting in 5 minutes\"", handler.LastRequestBody);
        Assert.DoesNotContain("\"embeds\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task NotifyServerStartedAsync_SendsAnEmbedWithTheSessionName()
    {
        var handler = new FakeHttpMessageHandler();
        var notifier = new DiscordWebhookNotifier(new HttpClient(handler), "https://discord.com/api/webhooks/123/abc");

        await notifier.NotifyServerStartedAsync("The Island - ArkKeeper");

        Assert.Contains("\"title\":\"Server started\"", handler.LastRequestBody);
        Assert.Contains("\"description\":\"The Island - ArkKeeper\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task SendAsync_OnNonSuccessStatus_Throws()
    {
        var handler = new FakeHttpMessageHandler { ResponseStatusCode = HttpStatusCode.TooManyRequests };
        var notifier = new DiscordWebhookNotifier(new HttpClient(handler), "https://discord.com/api/webhooks/123/abc");

        await Assert.ThrowsAsync<HttpRequestException>(() => notifier.SendAsync("hello"));
    }
}
