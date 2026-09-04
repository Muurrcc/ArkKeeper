using ArkKeeper.Networking.Processes;
using Xunit;

namespace ArkKeeper.Networking.Tests;

public class ProcessOutputPumpTests
{
    [Fact]
    public async Task PumpAsync_TreatsABareCarriageReturnAsALineTerminatorToo()
    {
        // SteamCMD (and many other CLI tools) report progress with '\r'-only updates meant to
        // overwrite one line in a real terminal — Process.OutputDataReceived/BeginOutputReadLine
        // only split on '\n' and would buffer "A" and "B" here until the final '\n', which is
        // exactly why a live install/download log looked frozen for long stretches.
        var lines = new List<string>();
        using var reader = new StringReader("A\rB\rC\n");

        await ProcessOutputPump.PumpAsync(reader, lines.Add);

        Assert.Equal(["A", "B", "C"], lines);
    }

    [Fact]
    public async Task PumpAsync_HandlesWindowsStyleCrLfWithoutEmittingABlankLine()
    {
        var lines = new List<string>();
        using var reader = new StringReader("A\r\nB\r\n");

        await ProcessOutputPump.PumpAsync(reader, lines.Add);

        Assert.Equal(["A", "B"], lines);
    }

    [Fact]
    public async Task PumpAsync_FlushesATrailingLineWithNoTerminator()
    {
        var lines = new List<string>();
        using var reader = new StringReader("no newline at the end");

        await ProcessOutputPump.PumpAsync(reader, lines.Add);

        Assert.Equal(["no newline at the end"], lines);
    }

    [Fact]
    public async Task PumpAsync_WithNoOutput_InvokesNothing()
    {
        var lines = new List<string>();
        using var reader = new StringReader(string.Empty);

        await ProcessOutputPump.PumpAsync(reader, lines.Add);

        Assert.Empty(lines);
    }
}
