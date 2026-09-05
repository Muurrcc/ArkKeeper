using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Scheduling;
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
            BackupScheduleEnabled = true,
            BackupScheduleKind = ScheduleKind.DailyAt,
            BackupScheduleValue = TimeSpan.FromHours(3.5),
            BackupCompress = false,
            BackupKeepCount = 5,
        };
        profile.ModIds.Add("111");
        profile.ModIds.Add("222");

        await store.SaveAsync(profile);
        var loaded = await store.LoadAllAsync();

        var reloaded = Assert.Single(loaded);
        Assert.Equivalent(profile.ToData(), reloaded.ToData());
    }

    [Fact]
    public async Task SaveAsync_CalledConcurrentlyForTheSameProfile_NeverThrows()
    {
        // File.Create defaults to FileShare.None — two overlapping SaveAsync calls for the same
        // profile (e.g. a user double-clicking Remove on two mod rows in quick succession, each
        // triggering its own save) could each try to open the same path for exclusive write at
        // once. Wrapped in WaitAsync so a regression hangs/fails loudly instead of flaking.
        var store = new ProfileStore(_directory);
        var profile = new ServerProfile { ProfileName = "Concurrent Save Test" };

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => store.SaveAsync(profile)))
            .WaitAsync(TimeSpan.FromSeconds(10));

        var loaded = await store.LoadAllAsync();
        Assert.Single(loaded);
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

    [Fact]
    public async Task LoadAllAsync_SkipsACorruptProfileFile_RatherThanThrowingForEveryProfile()
    {
        // A single truncated/corrupted *.json (a crash or disk-full mid-write, a leftover from
        // before SaveAsync's own concurrency fix, or a manually-edited file) must not take down
        // every other, perfectly good profile with it — and in this app specifically, LoadAllAsync
        // throwing here means MainViewModel.InitializeAsync throws, which is awaited from
        // MainWindow's "async void OnOpened" with no try/catch and no global handler: an unhandled
        // exception there crashes the whole app before the window is even usable, with no way to
        // reach Settings and fix it.
        var store = new ProfileStore(_directory);
        var goodProfile = new ServerProfile { ProfileName = "Good Profile" };
        await store.SaveAsync(goodProfile);

        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(Path.Combine(_directory, $"{Guid.NewGuid()}.json"), "{ not valid json");

        var loaded = await store.LoadAllAsync();

        var reloaded = Assert.Single(loaded);
        Assert.Equal(goodProfile.ProfileId, reloaded.ProfileId);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
