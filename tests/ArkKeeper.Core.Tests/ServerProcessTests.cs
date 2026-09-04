using ArkKeeper.Core.Profiles;
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
    public void ForProfile_Start_RefreshesArgumentsFromTheLiveProfileEachTime_NotJustAtConstruction()
    {
        // ServerProcess.ForProfile is called exactly once per profile — the ManagedServer wrapping
        // it is cached for the app's lifetime (ServerFleet.GetOrAdd) — so unless Start() re-reads
        // the profile every time, editing and saving a launch-only setting (BattlEye, ports,
        // mods, ...) after the server was first touched would silently keep using whatever the
        // profile looked like the first time, no matter how many times it's saved afterward. A
        // real bug a user hit: toggled "Disable BattlEye" and it had no effect on an already-known
        // profile until restarting the whole app.
        var profile = new ServerProfile { InstallDirectory = @"X:\does\not\exist\for\this\test", DisableBattlEye = false };
        var process = ServerProcess.ForProfile(profile);
        Assert.DoesNotContain("-NoBattlEye", process.Arguments);

        profile.DisableBattlEye = true;

        // The executable genuinely doesn't exist, so Start() throws — but RefreshFromProfile()
        // runs before that check, so Arguments is still updated by the time it does.
        Assert.Throws<FileNotFoundException>(() => process.Start());
        Assert.Contains("-NoBattlEye", process.Arguments);
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
