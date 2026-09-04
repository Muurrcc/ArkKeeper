using ArkKeeper.Core.Profiles;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class ServerProfileWriteConfigFilesTests : IDisposable
{
    private readonly string _installDirectory = Path.Combine(Path.GetTempPath(), "ArkKeeperWriteConfigTests_" + Guid.NewGuid());

    [Fact]
    public void WriteConfigFiles_WritesBothIniFilesUnderTheStandardConfigPath()
    {
        var profile = new ServerProfile { InstallDirectory = _installDirectory, XpMultiplier = 3.0f };

        profile.WriteConfigFiles();

        var configDirectory = Path.Combine(_installDirectory, "ShooterGame", "Saved", "Config", "WindowsServer");
        Assert.True(File.Exists(Path.Combine(configDirectory, "GameUserSettings.ini")));
        Assert.True(File.Exists(Path.Combine(configDirectory, "Game.ini")));
    }

    [Fact]
    public void WriteConfigFiles_ActuallyWritesKnownSettingValues()
    {
        var profile = new ServerProfile { InstallDirectory = _installDirectory, PveMode = true, XpMultiplier = 2.5f };

        profile.WriteConfigFiles();

        var text = File.ReadAllText(Path.Combine(_installDirectory, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini"));
        Assert.Contains("ServerPVE=True", text);
        Assert.Contains("XPMultiplier=2.5", text);
    }

    [Fact]
    public void WriteConfigFiles_WithNoInstallDirectory_DoesNothingRatherThanThrow()
    {
        // Start() calls this unconditionally; a not-yet-installed profile should just skip
        // writing rather than fail here — ServerProcess.Start() throws its own, clearer
        // "server executable not found" error right after anyway.
        var profile = new ServerProfile();

        profile.WriteConfigFiles();
    }

    [Fact]
    public void WriteConfigFiles_CalledAgainAfterAManualEditToTheIniFile_PreservesTheUnknownKeyInstead()
    {
        // This is the actual point of merging rather than overwriting: ARK has far more settings
        // than ServerProfile models, and a mod or a manual tweak may already be in this file.
        var profile = new ServerProfile { InstallDirectory = _installDirectory };
        profile.WriteConfigFiles();

        var gameUserSettingsPath = Path.Combine(_installDirectory, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");
        var withManualEdit = File.ReadAllText(gameUserSettingsPath) + "\n[SomeModSection]\nSomeModSetting=42\n";
        File.WriteAllText(gameUserSettingsPath, withManualEdit);

        profile.XpMultiplier = 5.0f;
        profile.WriteConfigFiles();

        var finalText = File.ReadAllText(gameUserSettingsPath);
        Assert.Contains("SomeModSetting=42", finalText);
        Assert.Contains("XPMultiplier=5", finalText);
    }

    [Fact]
    public void WriteConfigFiles_SyncsActiveModsFromModIds_LikeToGameUserSettingsDoes()
    {
        var profile = new ServerProfile { InstallDirectory = _installDirectory };
        profile.ModIds.Add("123456");

        profile.WriteConfigFiles();

        var text = File.ReadAllText(Path.Combine(_installDirectory, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini"));
        Assert.Contains("ActiveMods=123456", text);
    }

    public void Dispose()
    {
        if (Directory.Exists(_installDirectory))
        {
            Directory.Delete(_installDirectory, recursive: true);
        }
    }
}
