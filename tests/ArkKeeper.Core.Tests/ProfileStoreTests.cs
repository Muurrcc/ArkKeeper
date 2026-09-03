using ArkKeeper.Core.Profiles;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class ProfileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperTests_" + Guid.NewGuid());

    [Fact]
    public async Task SaveAndLoadAll_RoundTripsProfile()
    {
        var store = new ProfileStore(_directory);
        var profile = new ServerProfile { ProfileName = "Test Profile", SessionName = "Test Session" };

        await store.SaveAsync(profile);
        var loaded = await store.LoadAllAsync();

        var reloaded = Assert.Single(loaded);
        Assert.Equal(profile.ProfileId, reloaded.ProfileId);
        Assert.Equal("Test Profile", reloaded.ProfileName);
        Assert.Equal("Test Session", reloaded.SessionName);
    }

    [Fact]
    public async Task Delete_RemovesProfileFile()
    {
        var store = new ProfileStore(_directory);
        var profile = new ServerProfile();
        await store.SaveAsync(profile);

        store.Delete(profile.ProfileId);
        var loaded = await store.LoadAllAsync();

        Assert.Empty(loaded);
    }

    [Fact]
    public async Task LoadAllAsync_OnMissingDirectory_ReturnsEmpty()
    {
        var store = new ProfileStore(_directory);

        var loaded = await store.LoadAllAsync();

        Assert.Empty(loaded);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
