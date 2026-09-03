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
}
