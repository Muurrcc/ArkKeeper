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
    public void SampleResourceUsage_WhenNotRunning_ReturnsNull()
    {
        using var process = new ServerProcess(CmdExe, "/c ping -n 3 127.0.0.1 >nul");

        Assert.Null(process.SampleResourceUsage());
    }

    [Fact]
    public void SampleResourceUsage_WhenRunning_ReportsRealMemoryAndZeroCpuOnTheFirstSample()
    {
        // CPU% is derived from a delta against the previous sample — with no previous sample yet,
        // it can only honestly report 0 rather than a fabricated number. RAM needs no delta, so
        // it's accurate immediately.
        using var process = new ServerProcess(CmdExe, "/c ping -n 5 127.0.0.1 >nul");
        process.Start();

        var sample = process.SampleResourceUsage();

        Assert.NotNull(sample);
        Assert.Equal(0, sample!.Value.CpuPercent);
        Assert.True(sample.Value.WorkingSetBytes > 0);

        process.Kill();
    }

    [Fact]
    public async Task SampleResourceUsage_AfterProcessExits_ReturnsNullAgain()
    {
        using var process = new ServerProcess(CmdExe, "/c ping -n 5 127.0.0.1 >nul");
        process.Start();
        process.SampleResourceUsage();

        process.Kill();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Null(process.SampleResourceUsage());
    }

    [Fact]
    public async Task SampleResourceUsage_AfterARestart_ReportsZeroCpuAgainRatherThanAStaleDelta()
    {
        // Start() now resets the CPU-sample baseline — without that, the first sample after a
        // restart would diff the new process's TotalProcessorTime against the *previous*
        // process's timestamp/CPU-time from before it was killed, producing a bogus (likely huge
        // or negative, since it's comparing two unrelated processes' clocks) CPU% instead of an
        // honest 0.
        using var process = new ServerProcess(CmdExe, "/c ping -n 5 127.0.0.1 >nul");
        process.Start();
        process.SampleResourceUsage();
        await Task.Delay(200);
        process.SampleResourceUsage(); // non-zero-ish baseline established for the first process

        process.Kill();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

        process.Start();
        var sampleRightAfterRestart = process.SampleResourceUsage();

        Assert.NotNull(sampleRightAfterRestart);
        Assert.Equal(0, sampleRightAfterRestart!.Value.CpuPercent);

        process.Kill();
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
