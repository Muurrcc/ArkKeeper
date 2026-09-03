using ArkKeeper.Networking.SteamCmd;
using Xunit;

namespace ArkKeeper.Networking.Tests;

public class SteamCmdClientTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperSteamCmdClientTests_" + Guid.NewGuid());
    private static readonly string CmdExe = Environment.GetEnvironmentVariable("ComSpec") ?? @"C:\Windows\System32\cmd.exe";

    [Fact]
    public void BuildArguments_IncludesAppIdInstallDirAndAnonymousLogin()
    {
        var arguments = SteamCmdClient.BuildArguments(@"C:\Servers\MyArk");

        Assert.Contains("+force_install_dir \"C:\\Servers\\MyArk\"", arguments);
        Assert.Contains("+login anonymous", arguments);
        Assert.Contains($"+app_update {SteamCmdClient.ArkDedicatedServerAppId} validate", arguments);
        Assert.EndsWith("+quit", arguments);
    }

    [Fact]
    public async Task InstallOrUpdateAsync_RunsTheGivenExecutableAndReturnsAnExitCode()
    {
        // cmd.exe stands in for steamcmd.exe here purely to verify our process-invocation
        // plumbing (it starts, runs to completion, and produces an exit code) — it won't
        // understand steamcmd's own +arg syntax, and that's fine, correctness of the actual
        // arguments is covered separately by BuildArguments_* above.
        var client = new SteamCmdClient(CmdExe);

        var exitCode = await client.InstallOrUpdateAsync(_directory);

        Assert.True(exitCode is >= -1 and <= 255, $"Unexpected exit code: {exitCode}");
    }

    [Fact]
    public async Task InstallOrUpdateAsync_CreatesInstallDirectory()
    {
        var client = new SteamCmdClient(CmdExe);

        await client.InstallOrUpdateAsync(_directory);

        Assert.True(Directory.Exists(_directory));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
