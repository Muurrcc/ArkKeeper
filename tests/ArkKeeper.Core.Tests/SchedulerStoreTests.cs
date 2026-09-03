using ArkKeeper.Core.Scheduling;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class SchedulerStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperSchedulerStoreTests_" + Guid.NewGuid());
    private readonly string _filePath;

    public SchedulerStoreTests() => _filePath = Path.Combine(_directory, "scheduler.json");

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsEmpty()
    {
        var store = new SchedulerStore(_filePath);

        var tasks = await store.LoadAsync();

        Assert.Empty(tasks);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsMultipleTasks()
    {
        var store = new SchedulerStore(_filePath);
        var tasks = new List<ScheduledTask>
        {
            new("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(6)),
            new("Restart", "DoExit", ScheduleKind.DailyAt, TimeSpan.FromHours(4)),
        };

        await store.SaveAsync(tasks);
        var loaded = await store.LoadAsync();

        Assert.Equal(tasks, loaded);
    }

    [Fact]
    public async Task SaveAsync_CreatesTheDirectoryIfMissing()
    {
        var store = new SchedulerStore(_filePath);

        await store.SaveAsync(new List<ScheduledTask> { new("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1)) });

        Assert.True(File.Exists(_filePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
