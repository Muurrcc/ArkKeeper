using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArkKeeper.Core.Scheduling;

/// <summary>Persists a set of <see cref="ScheduledTask"/>s as a single JSON file, so they survive
/// an app restart instead of only living in a <c>SchedulerRunner</c>'s in-memory list.</summary>
public sealed class SchedulerStore
{
    private readonly string _filePath;

    // File.Create defaults to FileShare.None — SchedulerViewModel's AddAsync/RemoveAsync have no
    // busy-guard, so rapidly removing two different scheduled tasks for the same server can call
    // SaveAsync twice concurrently for this same file. One instance always owns exactly one file,
    // so a single semaphore (not keyed, unlike ProfileStore) is enough.
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public SchedulerStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<ScheduledTask>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<ScheduledTask>();
        }

        await using var stream = File.OpenRead(_filePath);
        var tasks = await JsonSerializer.DeserializeAsync(stream, SchedulerJsonContext.Default.ListScheduledTask, cancellationToken);
        return (IReadOnlyList<ScheduledTask>?)tasks ?? Array.Empty<ScheduledTask>();
    }

    public async Task SaveAsync(IReadOnlyList<ScheduledTask> tasks, CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, tasks.ToList(), SchedulerJsonContext.Default.ListScheduledTask, cancellationToken);
        }
        finally
        {
            _saveLock.Release();
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<ScheduledTask>))]
internal sealed partial class SchedulerJsonContext : JsonSerializerContext
{
}
