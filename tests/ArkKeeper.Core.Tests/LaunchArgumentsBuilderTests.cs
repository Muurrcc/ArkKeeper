using ArkKeeper.Core.Launch;
using ArkKeeper.Core.Profiles;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class LaunchArgumentsBuilderTests
{
    [Fact]
    public void Build_IncludesMapAndPorts()
    {
        var profile = new ServerProfile
        {
            MapName = "Ragnarok",
            Port = 7778,
            QueryPort = 27016,
        };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.StartsWith("Ragnarok?listen?Port=7778", args);
        Assert.Contains("QueryPort=27016", args);
    }

    [Fact]
    public void Build_NeverPutsSessionNameOnTheCommandLine()
    {
        // SessionName is already written to GameUserSettings.ini by WriteConfigFiles() (called
        // right before every Start()) — it used to also appear here, quoted, but a value like this
        // one containing both spaces and a quote can't be escaped by simply wrapping it, and
        // there's no reason to duplicate it here at all when the ini path already handles it.
        var profile = new ServerProfile { SessionName = "My \"Best\" Server" };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.DoesNotContain("SessionName", args);
    }

    [Fact]
    public void Build_NeverPutsServerPasswordOnTheCommandLine()
    {
        // Same reasoning as AdminPassword below: this used to appear here completely unquoted, so
        // a password containing a space would fracture the whole url-parameter blob into multiple
        // command-line arguments and corrupt the launch. ServerPassword is already written to
        // GameUserSettings.ini by WriteConfigFiles(), so it doesn't need to be here at all.
        var profile = new ServerProfile { ServerPassword = "my pass" };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.DoesNotContain("ServerPassword", args);
    }

    [Fact]
    public void Build_NeverPutsAdminPasswordOnTheCommandLine()
    {
        // Matches the original ARK Server Manager's own GetServerArgs(), which never put this on
        // the command line either — only in the ini. Putting it here was a real bug (a password
        // with a space corrupts the whole launch) and a real exposure (a process's command line
        // is visible to anything that can query it — Task Manager's "Command line" column, WMI).
        var profile = new ServerProfile { AdminPassword = "super secret" };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.DoesNotContain("AdminPassword", args);
        Assert.DoesNotContain("super secret", args);
    }

    [Fact]
    public void Build_WithMods_AppendsModsFlag()
    {
        var profile = new ServerProfile();
        profile.ModIds.Add("123456");
        profile.ModIds.Add("789012");

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.Contains("-mods=123456,789012", args);
    }

    [Fact]
    public void Build_WithoutMods_OmitsModsFlag()
    {
        var profile = new ServerProfile();

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.DoesNotContain("-mods=", args);
    }

    [Fact]
    public void Build_WhenRconDisabled_OmitsRconFlags()
    {
        var profile = new ServerProfile { RconEnabled = false };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.DoesNotContain("-RCONPort=", args);
    }

    [Fact]
    public void Build_WhenBattlEyeDisabled_AddsNoBattlEyeFlag()
    {
        var profile = new ServerProfile { DisableBattlEye = true };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.Contains("-NoBattlEye", args);
    }

    [Fact]
    public void Build_WhenBattlEyeNotDisabled_OmitsNoBattlEyeFlag()
    {
        var profile = new ServerProfile { DisableBattlEye = false };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.DoesNotContain("-NoBattlEye", args);
    }

    [Fact]
    public void Build_AlwaysIncludesMaxPlayersOnTheCommandLine()
    {
        // ARK's dedicated server is known to ignore MaxPlayers when it's only set in
        // GameUserSettings.ini — it has to be on the launch command line to actually apply.
        var profile = new ServerProfile { MaxPlayers = 42 };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.Contains("MaxPlayers=42", args);
    }

    [Fact]
    public void Build_WithServerIPSet_AddsMultiHomeFlag()
    {
        var profile = new ServerProfile { ServerIP = "10.0.0.5" };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.Contains("MultiHome=10.0.0.5", args);
    }

    [Fact]
    public void Build_WithoutServerIP_OmitsMultiHomeFlag()
    {
        var profile = new ServerProfile { ServerIP = "" };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.DoesNotContain("MultiHome=", args);
    }
}
