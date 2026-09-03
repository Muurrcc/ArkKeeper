using System.Text.Json.Serialization;

namespace ArkKeeper.Core.Settings;

/// <summary>ArkKeeper's own app-level settings — separate from any <see cref="Profiles.ServerProfile"/>.
/// A plain hand-written class (not an ObservableObject), so its JsonSerializerContext works
/// without the interop issue documented on ServerProfileData.</summary>
public sealed class AppSettings
{
    /// <summary>Where new profiles default their SteamCMD install to, unless overridden per-profile.</summary>
    public string DefaultInstallDirectory { get; set; } = string.Empty;

    /// <summary>Directory containing (or to download) steamcmd.exe.</summary>
    public string SteamCmdDirectory { get; set; } = string.Empty;

    /// <summary>Default Discord webhook URL for server notifications, used when a profile doesn't
    /// have its own.</summary>
    public string? DiscordWebhookUrl { get; set; }

    public bool DarkTheme { get; set; } = true;

    public string AccentColorHex { get; set; } = "#0FC2C0";
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext
{
}
