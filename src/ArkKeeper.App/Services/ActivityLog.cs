using System.Collections.ObjectModel;

namespace ArkKeeper.App.Services;

public enum ActivityKind
{
    Server,
    Backup,
}

public sealed record ActivityEntry(string Message, ActivityKind Kind, DateTimeOffset Timestamp);

/// <summary>A small, in-memory, session-only feed of real events (server started/stopped, backups
/// created/restored) — feeds the Dashboard's "Activity" card. Deliberately not persisted: it's a
/// live "what just happened" view, not a history log, and doesn't need to survive a restart.</summary>
public sealed class ActivityLog
{
    private const int MaxEntries = 30;

    public ObservableCollection<ActivityEntry> Entries { get; } = new();

    public void Add(string message, ActivityKind kind)
    {
        Entries.Insert(0, new ActivityEntry(message, kind, DateTimeOffset.Now));
        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }
}
