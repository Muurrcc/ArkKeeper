using System.Collections.ObjectModel;
using ArkKeeper.Core.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>Create/edit form for one <see cref="ServerProfile"/>. Edits the profile instance in
/// place rather than a working copy: for an existing profile, that instance is the exact one a
/// <c>ManagedServer</c> in the fleet already holds a reference to, so an in-place edit is visible
/// to it immediately (e.g. on the next Start) — copying then swapping the reference in the
/// profiles collection would leave that ManagedServer pointing at stale settings until the app
/// restarts. Trade-off: Cancel doesn't revert in-memory edits mid-session — nothing reaches disk
/// until Save runs, so a restart still discards anything not saved.</summary>
public partial class ProfileEditorViewModel : ViewModelBase
{
    private readonly ObservableCollection<ServerProfile> _profiles;
    private readonly ProfileStore _profileStore;
    private readonly Action _onClose;

    public ProfileEditorViewModel(
        ServerProfile? existing,
        ObservableCollection<ServerProfile> profiles,
        ProfileStore profileStore,
        Action onClose)
    {
        _profiles = profiles;
        _profileStore = profileStore;
        _onClose = onClose;
        IsNew = existing is null;
        Profile = existing ?? new ServerProfile();
    }

    public ServerProfile Profile { get; }

    public bool IsNew { get; }

    public IReadOnlyList<string> AvailableMaps { get; } =
    [
        "TheIsland", "TheCenter", "ScorchedEarth_P", "Ragnarok", "Aberration_P",
        "Extinction", "Valguero_P", "Genesis", "CrystalIsles", "Genesis2", "LostIsland", "Fjordur",
    ];

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Profile.ProfileName))
        {
            ErrorMessage = "Profile name is required.";
            return;
        }

        try
        {
            await _profileStore.SaveAsync(Profile);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't save: {ex.Message}";
            return;
        }

        if (IsNew)
        {
            _profiles.Add(Profile);
        }

        _onClose();
    }

    [RelayCommand]
    private void Cancel() => _onClose();
}
