using ArkKeeper.Core.Profiles;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class ServerProfileTests
{
    [Fact]
    public void NewProfile_GeneratesDistinctRandomPasswords()
    {
        var profile = new ServerProfile();

        Assert.NotEmpty(profile.ServerPassword);
        Assert.NotEmpty(profile.AdminPassword);
        Assert.NotEqual(profile.ServerPassword, profile.AdminPassword);
    }

    [Fact]
    public void ToGameUserSettings_WritesOnlyGameUserSettingsKeys_NotGameIniKeys()
    {
        // MaxPlayers lives in GameUserSettings.ini's /Script/Engine.GameSession section
        // (verified against the original tool's source — an earlier version of this code had
        // it in Game.ini, which was wrong), while e.g. MaxTribeLogs is a genuine Game.ini key.
        var profile = new ServerProfile { SessionName = "Island Server", MaxPlayers = 30, MaxTribeLogs = 50 };

        var document = profile.ToGameUserSettings();

        Assert.Equal("Island Server", document.FindSection("SessionSettings")!.GetSingle("SessionName"));
        Assert.Equal("30", document.FindSection("/Script/Engine.GameSession")!.GetSingle("MaxPlayers"));
        Assert.Null(document.FindSection("/script/shootergame.shootergamemode"));
    }

    [Fact]
    public void ToGameIni_WritesOnlyGameIniKeys()
    {
        var profile = new ServerProfile { MaxTribeLogs = 200 };

        var document = profile.ToGameIni();

        Assert.Equal("200", document.FindSection("/script/shootergame.shootergamemode")!.GetSingle("MaxTribeLogs"));
        Assert.Null(document.FindSection("SessionSettings"));
        Assert.Null(document.FindSection("/Script/Engine.GameSession"));
    }

    [Fact]
    public void ImportFrom_RoundTripsThroughIniText()
    {
        var original = new ServerProfile
        {
            SessionName = "Round Trip Server",
            Port = 7778,
            XpMultiplier = 3.0f,
            PveMode = true,
            MaxPlayers = 55,
        };

        var gameUserSettingsText = original.ToGameUserSettings().ToString();
        var gameText = original.ToGameIni().ToString();

        var restored = new ServerProfile();
        restored.ImportFrom(
            ArkKeeper.Core.Ini.IniDocument.Parse(gameUserSettingsText),
            ArkKeeper.Core.Ini.IniDocument.Parse(gameText));

        Assert.Equal(original.SessionName, restored.SessionName);
        Assert.Equal(original.Port, restored.Port);
        Assert.Equal(original.XpMultiplier, restored.XpMultiplier);
        Assert.Equal(original.PveMode, restored.PveMode);
        Assert.Equal(original.MaxPlayers, restored.MaxPlayers);
    }

    [Fact]
    public void ToGameUserSettings_WritesModIdsAsActiveMods()
    {
        // ModIds (used for the -mods= launch argument) and ActiveMods/ServerModIds (the ini key)
        // are two independent pieces of state — nothing keeps them in sync as ModIds changes, so
        // ToGameUserSettings has to derive the ini value from ModIds itself at write time.
        var profile = new ServerProfile();
        profile.ModIds.Add("123456");
        profile.ModIds.Add("789012");

        var document = profile.ToGameUserSettings();

        Assert.Equal("123456,789012", document.FindSection("ServerSettings")!.GetSingle("ActiveMods"));
    }

    [Fact]
    public void ImportFrom_PopulatesModIdsFromActiveMods()
    {
        var original = new ServerProfile();
        original.ModIds.Add("111");
        original.ModIds.Add("222");

        var restored = new ServerProfile();
        restored.ImportFrom(
            ArkKeeper.Core.Ini.IniDocument.Parse(original.ToGameUserSettings().ToString()),
            ArkKeeper.Core.Ini.IniDocument.Parse(original.ToGameIni().ToString()));

        Assert.Equal(new[] { "111", "222" }, restored.ModIds);
    }
}
