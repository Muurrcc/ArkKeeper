using System.Collections.ObjectModel;
using ArkKeeper.App.Services;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Scheduling;
using ArkKeeper.Core.Snapshots;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>World save/restore for one server, plus configuring its recurring backup schedule
/// (backed by <see cref="ArkKeeper.Orchestration.BackupScheduler"/>, actually run from
/// <see cref="ServerRowViewModel.RunDueBackupAsync"/> on the same background poll as everything
/// else). Backups live under <c>%AppData%/ArkKeeper/Backups/&lt;ProfileId&gt;</c> — a UI-level
/// default, since <see cref="WorldBackupService"/> takes an arbitrary root directory and nothing
/// about it is persisted on <see cref="ServerProfile"/> itself (see the backend's own doc comments).</summary>
public partial class BackupsViewModel : ViewModelBase
{
    private readonly ServerRowViewModel _server;
    private readonly ProfileStore _profileStore;
    private readonly WorldBackupService _backupService;
    private readonly ActivityLog _activityLog;
    private readonly Action _onClose;

    public BackupsViewModel(ServerRowViewModel server, ProfileStore profileStore, ActivityLog activityLog, Action onClose)
    {
        _server = server;
        _profileStore = profileStore;
        _activityLog = activityLog;
        _onClose = onClose;

        var backupRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArkKeeper", "Backups", server.Profile.ProfileId.ToString());
        _backupService = new WorldBackupService(backupRoot);

        IsBackupScheduleEnabled = Profile.BackupScheduleEnabled;
        BackupScheduleKind = Profile.BackupScheduleKind;
        BackupScheduleValueText = Profile.BackupScheduleKind == ScheduleKind.Interval
            ? Profile.BackupScheduleValue.TotalHours.ToString("0.##")
            : Profile.BackupScheduleValue.ToString(@"hh\:mm");
        BackupCompress = Profile.BackupCompress;
        BackupKeepCountText = Profile.BackupKeepCount > 0 ? Profile.BackupKeepCount.ToString() : string.Empty;

        LoadBackups();
    }

    public ServerProfile Profile => _server.Profile;

    public ObservableCollection<BackupRowViewModel> Backups { get; } = new();

    public IReadOnlyList<ScheduleKind> ScheduleKinds { get; } = Enum.GetValues<ScheduleKind>();

    [ObservableProperty]
    public partial bool HasNoBackups { get; set; } = true;

    [ObservableProperty]
    public partial bool CompressNewBackups { get; set; } = true;

    [ObservableProperty]
    public partial bool IsBackupScheduleEnabled { get; set; }

    [ObservableProperty]
    public partial ScheduleKind BackupScheduleKind { get; set; }

    /// <summary>Hours between backups when <see cref="BackupScheduleKind"/> is Interval, or a
    /// time of day (HH:mm) when it's DailyAt — same text-entry shape as the Scheduler page's
    /// own interval/time field.</summary>
    [ObservableProperty]
    public partial string BackupScheduleValueText { get; set; } = "6";

    [ObservableProperty]
    public partial bool BackupCompress { get; set; } = true;

    /// <summary>Empty means keep every scheduled backup.</summary>
    [ObservableProperty]
    public partial string BackupKeepCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateBackupCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
    public partial bool IsBusy { get; set; }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task CreateBackupAsync()
    {
        ErrorMessage = null;
        StatusMessage = null;
        IsBusy = true;
        try
        {
            if (_server.IsRunning)
            {
                // Best-effort — an unreachable RCON connection shouldn't block taking a backup of
                // whatever's already on disk, just mean it might be a little stale.
                try
                {
                    await _server.SendRconCommandAsync("SaveWorld");
                }
                catch
                {
                }
            }

            var path = _backupService.CreateBackup(Profile.GetSaveDirectory(), compress: CompressNewBackups);
            StatusMessage = $"Backup created: {System.IO.Path.GetFileName(path)}";
            _activityLog.Add($"Backup completed for {Profile.ProfileName}", ActivityKind.Backup);
            LoadBackups();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task RestoreAsync(BackupRowViewModel backup)
    {
        ErrorMessage = null;
        StatusMessage = null;

        if (_server.IsRunning)
        {
            ErrorMessage = "Stop the server before restoring a backup.";
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Run(() => _backupService.RestoreBackup(backup.Path, Profile.GetSaveDirectory()));
            StatusMessage = $"Restored {backup.Timestamp}.";
            _activityLog.Add($"Backup restored for {Profile.ProfileName}", ActivityKind.Backup);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Reload() => LoadBackups();

    /// <summary>Persists the backup-schedule settings onto the profile and rebuilds
    /// <see cref="ServerRowViewModel"/>'s <c>BackupScheduler</c> immediately, so a change here
    /// takes effect on the very next poll tick instead of waiting for an app restart.</summary>
    [RelayCommand]
    private async Task SaveScheduleAsync()
    {
        ErrorMessage = null;
        StatusMessage = null;

        TimeSpan value;
        if (BackupScheduleKind == ScheduleKind.Interval)
        {
            if (!double.TryParse(BackupScheduleValueText, out var hours) || hours <= 0)
            {
                ErrorMessage = "Enter a positive number of hours for the interval.";
                return;
            }

            value = TimeSpan.FromHours(hours);
        }
        else
        {
            if (!TimeSpan.TryParse(BackupScheduleValueText, out value))
            {
                ErrorMessage = "Enter a time of day as HH:mm.";
                return;
            }
        }

        var keepCount = 0;
        if (!string.IsNullOrWhiteSpace(BackupKeepCountText) && (!int.TryParse(BackupKeepCountText, out keepCount) || keepCount < 0))
        {
            ErrorMessage = "Keep count must be a non-negative number, or blank to keep every backup.";
            return;
        }

        Profile.BackupScheduleEnabled = IsBackupScheduleEnabled;
        Profile.BackupScheduleKind = BackupScheduleKind;
        Profile.BackupScheduleValue = value;
        Profile.BackupCompress = BackupCompress;
        Profile.BackupKeepCount = keepCount;

        await _profileStore.SaveAsync(Profile);
        _server.RefreshBackupSchedule();
        StatusMessage = IsBackupScheduleEnabled ? "Backup schedule saved." : "Automatic backups disabled.";
    }

    private bool CanAct() => !IsBusy;

    private void LoadBackups()
    {
        Backups.Clear();
        foreach (var path in _backupService.ListBackups())
        {
            Backups.Add(new BackupRowViewModel(path));
        }

        HasNoBackups = Backups.Count == 0;
    }

    [RelayCommand]
    private void Close() => _onClose();
}
