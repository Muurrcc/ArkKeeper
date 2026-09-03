using ArkKeeper.Core.Profiles;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class ServerProfileImportDuplicateTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperImportTests_" + Guid.NewGuid());

    public ServerProfileImportDuplicateTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Duplicate_ProducesAnIndependentCopyWithADifferentProfileId()
    {
        var original = new ServerProfile { ProfileName = "Original", SessionName = "My Server", Port = 7778 };

        var copy = original.Duplicate();

        Assert.NotEqual(original.ProfileId, copy.ProfileId);
        Assert.Equal("Original (copy)", copy.ProfileName);
        Assert.Equal(original.SessionName, copy.SessionName);
        Assert.Equal(original.Port, copy.Port);
    }

    [Fact]
    public void Duplicate_WithExplicitName_UsesThatName()
    {
        var original = new ServerProfile { ProfileName = "Original" };

        var copy = original.Duplicate("Renamed Copy");

        Assert.Equal("Renamed Copy", copy.ProfileName);
    }

    [Fact]
    public void Duplicate_MutatingTheCopysListDoesNotAffectTheOriginal()
    {
        var original = new ServerProfile();
        original.ModIds.Add("111");

        var copy = original.Duplicate();
        copy.ModIds.Add("222");

        Assert.Single(original.ModIds);
        Assert.Equal(2, copy.ModIds.Count);
    }

    [Fact]
    public async Task ImportFromDirectoryAsync_ReadsBothIniFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "GameUserSettings.ini"),
            "[SessionSettings]\nSessionName=Imported Server\nPort=7779\n\n[/Script/Engine.GameSession]\nMaxPlayers=42\n");
        await File.WriteAllTextAsync(Path.Combine(_directory, "Game.ini"), string.Empty);

        var profile = await ServerProfile.ImportFromDirectoryAsync(_directory);

        Assert.Equal("Imported Server", profile.SessionName);
        Assert.Equal(7779, profile.Port);
        Assert.Equal(42, profile.MaxPlayers);
    }

    [Fact]
    public async Task ImportFromDirectoryAsync_WithMissingFiles_UsesDefaultsInstead()
    {
        var profile = await ServerProfile.ImportFromDirectoryAsync(_directory);

        Assert.Equal("My ArkKeeper Server", profile.SessionName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
