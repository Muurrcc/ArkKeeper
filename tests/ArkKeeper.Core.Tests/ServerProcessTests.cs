using ArkKeeper.Core.Servers;
using Xunit;

namespace ArkKeeper.Core.Tests;

// Uses cmd.exe as a harmless stand-in for the ARK server executable, to exercise the real
// Process wrapper (start, status, exit event, kill) rather than mocking it away — the same
// reasoning as the RCON/Steam tests: prove the actual plumbing works, not just that it compiles.
public class ServerProcessTests
{
    private static readonly string CmdExe = Environment.GetEnvironmentVariable("ComSpec") ?? @"C:\Windows\System32\cmd.exe";

    [Fact]
    public void Start_LaunchesProcess_StatusBecomesRunning()
    {
        using var process = new ServerProcess(CmdExe, "/c ping -n 3 127.0.0.1 >nul");

        process.Start();

        Assert.Equal(ServerStatus.Running, process.Status);
        Assert.NotNull(process.ProcessId);

        process.Kill();
    }

    [Fact]
    public void Start_WhenAlreadyRunning_Throws()
    {
        using var process = new ServerProcess(CmdExe, "/c ping -n 3 127.0.0.1 >nul");
        process.Start();

        Assert.Throws<InvalidOperationException>(() => process.Start());

        process.Kill();
    }

    [Fact]
    public void Start_WithMissingExecutable_ThrowsFileNotFound()
    {
        using var process = new ServerProcess(@"X:\does\not\exist.exe", "");

        Assert.Throws<FileNotFoundException>(() => process.Start());
    }

    [Fact]
    public async Task Exited_FiresWhenProcessTerminatesOnItsOwn()
    {
        using var process = new ServerProcess(CmdExe, "/c exit 0");
        var exitedSignal = new TaskCompletionSource();
        process.Exited += (_, _) => exitedSignal.TrySetResult();

        process.Start();
        await exitedSignal.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(ServerStatus.Stopped, process.Status);
    }

    [Fact]
    public async Task Kill_TerminatesRunningProcess()
    {
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        process.Start();
        Assert.Equal(ServerStatus.Running, process.Status);

        process.Kill();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(ServerStatus.Stopped, process.Status);
    }
}
