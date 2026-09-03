using ArkKeeper.Core.Profiles;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class ProfileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperTests_" + Guid.NewGuid());

    [Fact]
    public async Task SaveAndLoadAll_RoundTripsEveryField()
    {
        // Every field set to a non-default, distinguishable value on purpose: this is the exact
        // shape of bug a partial round-trip test would miss (JSON source-gen silently dropping
        // most properties — see ServerProfileData's doc comment for the real incident).
        var store = new ProfileStore(_directory);
        var profile = new ServerProfile
        {
            ProfileName = "Test Profile",
            SessionName = "Test Session",
            Port = 7778,
            QueryPort = 27016,
            ServerPassword = "server-pw",
            AdminPassword = "admin-pw",
            RconEnabled = false,
            RconPort = 27021,
            PveMode = true,
            Hardcore = true,
            ShowCrosshair = false,
            ShowMapPlayerLocation = false,
            AllowThirdPerson = false,
            DisableStructureDecayPve = true,
            DifficultyOffset = 0.5f,
            XpMultiplier = 2.5f,
            TamingSpeedMultiplier = 3.5f,
            HarvestAmountMultiplier = 4.5f,
            ResourcesRespawnPeriodMultiplier = 5.5f,
            DayCycleSpeedScale = 6.5f,
            DinoDamageMultiplier = 7.5f,
            PlayerDamageMultiplier = 8.5f,
            StructureDamageMultiplier = 9.5f,
            MaxPlayers = 42,
            MapName = "Ragnarok",
        };
        profile.ModIds.Add("111");
        profile.ModIds.Add("222");

        await store.SaveAsync(profile);
        var loaded = await store.LoadAllAsync();

        var reloaded = Assert.Single(loaded);
        Assert.Equivalent(profile.ToData(), reloaded.ToData());
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
