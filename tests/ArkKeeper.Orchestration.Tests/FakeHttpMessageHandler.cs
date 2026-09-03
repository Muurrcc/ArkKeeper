using System.Net;

namespace ArkKeeper.Orchestration.Tests;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<string> _requestBodies = new();

    public IReadOnlyList<string> RequestBodies
    {
        get { lock (_requestBodies) { return _requestBodies.ToArray(); } }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        lock (_requestBodies)
        {
            _requestBodies.Add(body);
        }
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }
}
