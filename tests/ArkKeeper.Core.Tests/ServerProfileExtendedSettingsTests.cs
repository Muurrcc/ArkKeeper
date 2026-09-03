using ArkKeeper.Core.Ini;
using ArkKeeper.Core.Profiles;
using Xunit;

namespace ArkKeeper.Core.Tests;

/// <summary>
/// Covers the ~186 settings ported from the original tool's source (ServerProfile.cs) on top
/// of the original ~26. Rather than asserting every single field (see ServerProfileData's doc
/// comment for why that class exists at all — a prior full-fields miss is exactly how a real
/// data-loss bug slipped through once already), this exercises a representative sample from
/// each of the three sections/files involved, plus a full JSON round-trip via ToData/FromData.
/// </summary>
public class ServerProfileExtendedSettingsTests
{
    [Fact]
    public void ExtendedServerSettings_WriteToCorrectSectionAndFile()
    {
        var profile = new ServerProfile
        {
            SpectatorPassword = "spectate-me",
            MaxTamedDinos = 5000,
            PreventOfflinePvP = true,
            DisableDinoDecayPvP = false,
        };

        var document = profile.ToGameUserSettings();
        var section = document.FindSection("ServerSettings")!;

        Assert.Equal("spectate-me", section.GetSingle("SpectatorPassword"));
        Assert.Equal("5000", section.GetSingle("MaxTamedDinos"));
        Assert.Equal("True", section.GetSingle("PreventOfflinePvP"));
        Assert.Equal("False", section.GetSingle("PvPDinoDecay"));
    }

    [Fact]
    public void ExtendedGameModeSettings_WriteToCorrectSectionAndFile()
    {
        var profile = new ServerProfile
        {
            DisableFriendlyFirePvP = true,
            EggHatchSpeedMultiplier = 5.5f,
            LimitTurretsNum = 250,
        };

        var document = profile.ToGameIni();
        var section = document.FindSection("/script/shootergame.shootergamemode")!;

        Assert.Equal("True", section.GetSingle("bDisableFriendlyFire"));
        Assert.Equal("5.5", section.GetSingle("EggHatchSpeedMultiplier"));
        Assert.Equal("250", section.GetSingle("LimitTurretsNum"));
    }

    [Fact]
    public void MessageOfTheDay_WritesToItsOwnSection()
    {
        var profile = new ServerProfile { MOTD = "Welcome!", MOTDDuration = 45 };

        var document = profile.ToGameUserSettings();
        var section = document.FindSection("MessageOfTheDay")!;

        Assert.Equal("Welcome!", section.GetSingle("Message"));
        Assert.Equal("45", section.GetSingle("Duration"));
    }

    [Fact]
    public void ImportFrom_RoundTripsExtendedSettingsThroughIniText()
    {
        var original = new ServerProfile
        {
            SpectatorPassword = "abc123",
            MaxTamedDinos = 3000,
            DisableFriendlyFirePvP = true,
            BabyMatureSpeedMultiplier = 10.0f,
            MOTD = "Round trip MOTD",
        };

        var gameUserSettingsText = original.ToGameUserSettings().ToString();
        var gameText = original.ToGameIni().ToString();

        var restored = new ServerProfile();
        restored.ImportFrom(IniDocument.Parse(gameUserSettingsText), IniDocument.Parse(gameText));

        Assert.Equal(original.SpectatorPassword, restored.SpectatorPassword);
        Assert.Equal(original.MaxTamedDinos, restored.MaxTamedDinos);
        Assert.Equal(original.DisableFriendlyFirePvP, restored.DisableFriendlyFirePvP);
        Assert.Equal(original.BabyMatureSpeedMultiplier, restored.BabyMatureSpeedMultiplier);
        Assert.Equal(original.MOTD, restored.MOTD);
    }

    [Fact]
    public void ToData_ThenFromData_RoundTripsExtendedSettings()
    {
        var original = new ServerProfile
        {
            SpectatorPassword = "xyz",
            MaxTribeLogs = 250,
            AllowCustomRecipes = false,
            CraftXPMultiplier = 2.5f,
            PGM_Name = "MyProceduralMap",
        };
        original.ModIds.Add("999");

        var restored = ServerProfile.FromData(original.ToData());

        Assert.Equivalent(original.ToData(), restored.ToData());
    }
}
