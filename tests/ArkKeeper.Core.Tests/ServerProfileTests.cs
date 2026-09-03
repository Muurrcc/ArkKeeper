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
        var profile = new ServerProfile { SessionName = "Island Server", MaxPlayers = 30 };

        var document = profile.ToGameUserSettings();

        Assert.Equal("Island Server", document.FindSection("SessionSettings")!.GetSingle("SessionName"));
        Assert.Null(document.FindSection("/Script/Engine.GameSession"));
    }

    [Fact]
    public void ToGameIni_WritesOnlyGameIniKeys()
    {
        var profile = new ServerProfile { MaxPlayers = 42 };

        var document = profile.ToGameIni();

        Assert.Equal("42", document.FindSection("/Script/Engine.GameSession")!.GetSingle("MaxPlayers"));
        Assert.Null(document.FindSection("SessionSettings"));
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
}
