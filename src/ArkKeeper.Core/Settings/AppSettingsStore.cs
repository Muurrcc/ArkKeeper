using System.Text.Json;

namespace ArkKeeper.Core.Settings;

/// <summary>Persists <see cref="AppSettings"/> as a single JSON file.</summary>
public sealed class AppSettingsStore
{
    private readonly string _filePath;

    // File.Create defaults to FileShare.None — SettingsViewModel fires SaveAsync as fire-and-forget
    // ("_ = SaveAsync();") from every settings property setter, so two settings changed in quick
    // succession can call SaveAsync twice concurrently for this same file.
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public AppSettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new AppSettings();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var settings = await JsonSerializer.DeserializeAsync(stream, AppSettingsJsonContext.Default.AppSettings, cancellationToken);
            return settings ?? new AppSettings();
        }
        catch (JsonException)
        {
            // A truncated/corrupted settings file (crash, disk full, ...) is loaded from
            // MainViewModel.InitializeAsync via MainWindow's "async void OnOpened" with no
            // try/catch and no global handler — letting this throw would crash the whole app
            // before the window is even usable, with no way to reach Settings and fix it.
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
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
            await JsonSerializer.SerializeAsync(stream, settings, AppSettingsJsonContext.Default.AppSettings, cancellationToken);
        }
        finally
        {
            _saveLock.Release();
        }
    }
}
