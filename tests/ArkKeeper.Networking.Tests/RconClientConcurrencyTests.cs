using ArkKeeper.Networking.Rcon;
using Xunit;

namespace ArkKeeper.Networking.Tests;

public class RconClientConcurrencyTests
{
    [Fact]
    public async Task ExecuteCommandAsync_CalledConcurrently_EachCommandGetsItsOwnCorrectResponse()
    {
        // The lowest-level regression test for the bug found while building ManagedServer's
        // concurrency handling: without internal locking, two ExecuteCommandAsync calls on one
        // RconClient interleave their writes/reads on the shared stream and corrupt each
        // other — this hung indefinitely before RconClient serialized itself internally.
        // Wrapped in WaitAsync so a regression fails loudly instead of hanging the test run.
        await using var server = new FakeRconServer
        {
            ResponseProvider = cmd => $"response-to-{cmd}",
            ResponseDelay = TimeSpan.FromMilliseconds(30),
        };
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        var commands = Enumerable.Range(0, 15).Select(i => $"cmd{i}").ToArray();
        var results = await Task.WhenAll(commands.Select(c => rcon.ExecuteCommandAsync(c)))
            .WaitAsync(TimeSpan.FromSeconds(10));

        for (var i = 0; i < commands.Length; i++)
        {
            Assert.Equal($"response-to-{commands[i]}", results[i]);
        }
    }
}
