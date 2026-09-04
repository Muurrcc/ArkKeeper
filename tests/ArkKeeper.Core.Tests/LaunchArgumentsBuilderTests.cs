using ArkKeeper.Core.Launch;
using ArkKeeper.Core.Profiles;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class LaunchArgumentsBuilderTests
{
    [Fact]
    public void Build_IncludesMapSessionNameAndPorts()
    {
        var profile = new ServerProfile
        {
            MapName = "Ragnarok",
            SessionName = "My ArkKeeper Server",
            Port = 7778,
            QueryPort = 27016,
        };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.StartsWith("Ragnarok?listen?SessionName=\"My ArkKeeper Server\"", args);
        Assert.Contains("Port=7778", args);
        Assert.Contains("QueryPort=27016", args);
    }

    [Fact]
    public void Build_OmitsServerPasswordWhenEmpty()
    {
        var profile = new ServerProfile { ServerPassword = "" };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.DoesNotContain("ServerPassword=", args);
    }

    [Fact]
    public void Build_IncludesAdminPasswordAlways()
    {
        var profile = new ServerProfile { AdminPassword = "supersecret" };

        var args = LaunchArgumentsBuilder.Build(profile);

        Assert.Contains("ServerAdminPassword=supersecret", args);
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
