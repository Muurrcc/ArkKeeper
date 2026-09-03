using System.Net;

namespace ArkKeeper.Discord.Tests;

/// <summary>Captures the single request sent through it and returns a canned response,
/// so DiscordWebhookNotifier can be tested without any real network call.</summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.NoContent;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(ResponseStatusCode);
    }
}
