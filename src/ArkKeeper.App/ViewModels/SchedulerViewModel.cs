using System.Collections.ObjectModel;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Scheduling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>Recurring RCON commands for one server (SaveWorld, DoExit, a broadcast, ...) —
/// ArkKeeper's own in-process scheduler. Tasks run in the background via
/// <see cref="ServerRowViewModel.RunDueScheduledTasksAsync"/> on <see cref="MainViewModel"/>'s
/// poll timer, whether or not this page is open; this page only edits the list and shows run
/// history.</summary>
public partial class SchedulerViewModel : ViewModelBase
{
    private readonly ServerRowViewModel _server;
    private readonly Action _onClose;

    public SchedulerViewModel(ServerRowViewModel server, Action onClose)
    {
        _server = server;
        _onClose = onClose;
        RefreshTasks();
    }

    public ServerProfile Profile => _server.Profile;

    public ObservableCollection<ScheduleRowViewModel> Tasks { get; } = new();

    public IReadOnlyList<ScheduleKind> ScheduleKinds { get; } = Enum.GetValues<ScheduleKind>();

    [ObservableProperty]
    public partial bool HasNoTasks { get; set; } = true;

    [ObservableProperty]
    public partial string NewTaskName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewTaskCommand { get; set; } = "SaveWorld";

    [ObservableProperty]
    public partial ScheduleKind NewTaskKind { get; set; } = ScheduleKind.Interval;

    /// <summary>Hours between runs when <see cref="NewTaskKind"/> is Interval, or "HH:mm" time of
    /// day when it's DailyAt — parsed differently in <see cref="AddAsync"/> depending on which.</summary>
    [ObservableProperty]
    public partial string NewTaskValueText { get; set; } = "6";

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task AddAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(NewTaskName) || string.IsNullOrWhiteSpace(NewTaskCommand))
        {
            ErrorMessage = "Name and command are required.";
            return;
        }

        TimeSpan value;
        if (NewTaskKind == ScheduleKind.Interval)
        {
            if (!double.TryParse(NewTaskValueText, out var hours) || hours <= 0)
            {
                ErrorMessage = "Enter a positive number of hours for the interval.";
                return;
            }

            value = TimeSpan.FromHours(hours);
        }
        else
        {
            if (!TimeSpan.TryParse(NewTaskValueText, out value))
            {
                ErrorMessage = "Enter a time of day as HH:mm.";
                return;
            }
        }

        _server.Scheduler.Add(new ScheduledTask(NewTaskName, NewTaskCommand, NewTaskKind, value));
        await _server.SaveScheduleAsync();

        NewTaskName = string.Empty;
        RefreshTasks();
    }

    [RelayCommand]
    private async Task RemoveAsync(ScheduleRowViewModel row)
    {
        _server.Scheduler.Remove(row.Schedule);
        await _server.SaveScheduleAsync();
        RefreshTasks();
    }

    [RelayCommand]
    private void Close() => _onClose();

    /// <summary>Re-reads Next/Last run times from the live schedule. <see cref="ScheduleRowViewModel"/>
    /// snapshots those into plain, non-observable strings at construction time — without calling
    /// this again, a task that actually ran while this page was open (background execution
    /// doesn't stop just because a different page has focus) would show a stale "Last: Never" or
    /// yesterday's "Next:" forever, since nothing ever re-read the schedule after the page first
    /// opened. Called from <see cref="MainViewModel"/>'s poll tick while this is the open page.</summary>
    public void RefreshTasks()
    {
        Tasks.Clear();
        foreach (var schedule in _server.Scheduler.Schedules)
        {
            Tasks.Add(new ScheduleRowViewModel(schedule));
        }

        HasNoTasks = Tasks.Count == 0;
    }
}
