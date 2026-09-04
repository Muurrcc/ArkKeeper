using System.Collections.ObjectModel;
using ArkKeeper.App.Services;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Snapshots;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>World save/restore for one server. Backups live under
/// <c>%AppData%/ArkKeeper/Backups/&lt;ProfileId&gt;</c> — a UI-level default, since
/// <see cref="WorldBackupService"/> takes an arbitrary root directory and nothing about it is
/// persisted on <see cref="ServerProfile"/> itself (see the backend's own doc comments).</summary>
public partial class BackupsViewModel : ViewModelBase
{
    private readonly ServerRowViewModel _server;
    private readonly WorldBackupService _backupService;
    private readonly ActivityLog _activityLog;
    private readonly Action _onClose;

    public BackupsViewModel(ServerRowViewModel server, ActivityLog activityLog, Action onClose)
    {
        _server = server;
        _activityLog = activityLog;
        _onClose = onClose;

        var backupRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArkKeeper", "Backups", server.Profile.ProfileId.ToString());
        _backupService = new WorldBackupService(backupRoot);

        LoadBackups();
    }

    public ServerProfile Profile => _server.Profile;

    public ObservableCollection<BackupRowViewModel> Backups { get; } = new();

    [ObservableProperty]
    public partial bool HasNoBackups { get; set; } = true;

    [ObservableProperty]
    public partial bool CompressNewBackups { get; set; } = true;

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
