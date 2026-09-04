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

    [Fact]
    public void BuildWorkshopDownloadArguments_IncludesAppIdAndPublishedFileId()
    {
        var arguments = SteamCmdClient.BuildWorkshopDownloadArguments(@"C:\Servers\MyArk", "731604991");

        Assert.Contains("+force_install_dir \"C:\\Servers\\MyArk\"", arguments);
        Assert.Contains("+login anonymous", arguments);
        // Workshop items are published against the base game's app id, not the dedicated
        // server's — using the wrong one is why mods never actually reached a server.
        Assert.Contains($"+workshop_download_item {SteamCmdClient.ArkGameAppId} 731604991", arguments);
        Assert.EndsWith("+quit", arguments);
    }

    [Fact]
    public void GetWorkshopItemPath_MatchesSteamCmdsFixedConvention()
    {
        var path = SteamCmdClient.GetWorkshopItemPath(@"C:\Servers\MyArk", "731604991");

        Assert.Equal(
            Path.Combine(@"C:\Servers\MyArk", "steamapps", "workshop", "content", "346110", "731604991"),
            path);
    }

    [Fact]
    public void DeployDownloadedMod_CopiesContentIntoShooterGameContentMods()
    {
        var sourceContent = SteamCmdClient.GetWorkshopItemPath(_directory, "731604991");
        Directory.CreateDirectory(sourceContent);
        File.WriteAllText(Path.Combine(sourceContent, "mod.info"), "fake mod content");
        Directory.CreateDirectory(Path.Combine(sourceContent, "nested"));
        File.WriteAllText(Path.Combine(sourceContent, "nested", "asset.uasset"), "fake asset");

        SteamCmdClient.DeployDownloadedMod(_directory, "731604991");

        var deployed = Path.Combine(_directory, "ShooterGame", "Content", "Mods", "731604991");
        Assert.True(File.Exists(Path.Combine(deployed, "mod.info")));
        Assert.True(File.Exists(Path.Combine(deployed, "nested", "asset.uasset")));
    }

    [Fact]
    public void DeployDownloadedMod_CopiesTheSiblingModMetadataFile()
    {
        var workshopContentDir = Path.Combine(_directory, "steamapps", "workshop", "content", SteamCmdClient.ArkGameAppId.ToString());
        Directory.CreateDirectory(workshopContentDir);
        File.WriteAllText(Path.Combine(workshopContentDir, "731604991.mod"), "fake .mod metadata");

        SteamCmdClient.DeployDownloadedMod(_directory, "731604991");

        var deployedModFile = Path.Combine(_directory, "ShooterGame", "Content", "Mods", "731604991.mod");
        Assert.True(File.Exists(deployedModFile));
        Assert.Equal("fake .mod metadata", File.ReadAllText(deployedModFile));
    }

    [Fact]
    public void DeployDownloadedMod_WhenNothingWasDownloaded_DoesNothingRatherThanThrow()
    {
        SteamCmdClient.DeployDownloadedMod(_directory, "731604991");

        Assert.False(Directory.Exists(Path.Combine(_directory, "ShooterGame", "Content", "Mods")));
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_ActuallyKillsTheProcessInsteadOfJustAbandoningTheAwait()
    {
        // A long-running stand-in process (steamcmd itself can run for minutes on a real
        // download) — cancelling early and asserting the whole call returns quickly proves the
        // process was actually killed, not just that the await stopped waiting on it.
        var client = new SteamCmdClient(CmdExe);
        using var cts = new CancellationTokenSource();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var task = client.RunAsync("/c ping -n 30 127.0.0.1 >nul", onOutput: null, cts.Token);
        await Task.Delay(300);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Expected cancellation to kill the process quickly, took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task DownloadWorkshopItemAsync_RunsTheGivenExecutableAndCreatesInstallDirectory()
    {
        var client = new SteamCmdClient(CmdExe);

        var exitCode = await client.DownloadWorkshopItemAsync(_directory, "731604991");

        Assert.True(exitCode is >= -1 and <= 255, $"Unexpected exit code: {exitCode}");
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
