using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ArkKeeper.Discord;

/// <summary>Sends server event notifications to a Discord channel via an incoming webhook.
/// A webhook URL is all that's needed — no bot token, no Discord.Net dependency.</summary>
public sealed class DiscordWebhookNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;

    public DiscordWebhookNotifier(HttpClient httpClient, string webhookUrl)
    {
        _httpClient = httpClient;
        _webhookUrl = webhookUrl;
    }

    public Task SendAsync(string content, CancellationToken cancellationToken = default) =>
        SendAsync(content, embed: null, cancellationToken);

    public async Task SendAsync(string? content, DiscordEmbed? embed, CancellationToken cancellationToken = default)
    {
        var payload = new WebhookPayload(
            content,
            embed is null ? null : new[] { new WebhookEmbed(embed.Title, embed.Description, embed.ColorHex) });

        var json = JsonSerializer.Serialize(payload, WebhookPayloadJsonContext.Default.WebhookPayload);
        using var body = new StringContent(json, Encoding.UTF8);
        body.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _httpClient.PostAsync(_webhookUrl, body, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
