using System.Text.Json.Serialization;

namespace ArkKeeper.Core.Profiles;

/// <summary>
/// A plain, hand-written snapshot of everything a <see cref="ServerProfile"/> holds — used only
/// for JSON persistence via <see cref="ProfileStore"/>.
///
/// This exists because System.Text.Json's source-generated <see cref="JsonSerializerContext"/>
/// does not see properties CommunityToolkit.Mvvm's [ObservableProperty] generates on
/// <see cref="ServerProfile"/> (confirmed by inspecting the actual serialized output: it wrote
/// only ProfileId/ModIds, silently dropping every other setting). Since STJ's generator only
/// needs to see plain hand-written properties to work correctly, mapping through this type
/// keeps persistence both correct and trim/NativeAOT-safe, while ServerProfile itself stays an
/// ObservableObject for UI data binding.
/// </summary>
public sealed class ServerProfileData
{
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public int Port { get; set; }
    public int QueryPort { get; set; }
    public string ServerPassword { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public bool RconEnabled { get; set; }
    public int RconPort { get; set; }
    public bool PveMode { get; set; }
    public bool Hardcore { get; set; }
    public bool ShowCrosshair { get; set; }
    public bool ShowMapPlayerLocation { get; set; }
    public bool AllowThirdPerson { get; set; }
    public bool DisableStructureDecayPve { get; set; }
    public float DifficultyOffset { get; set; }
    public float XpMultiplier { get; set; }
    public float TamingSpeedMultiplier { get; set; }
    public float HarvestAmountMultiplier { get; set; }
    public float ResourcesRespawnPeriodMultiplier { get; set; }
    public float DayCycleSpeedScale { get; set; }
    public float DinoDamageMultiplier { get; set; }
    public float PlayerDamageMultiplier { get; set; }
    public float StructureDamageMultiplier { get; set; }
    public int MaxPlayers { get; set; }
    public string MapName { get; set; } = string.Empty;
    public List<string> ModIds { get; set; } = new();
    public string InstallDirectory { get; set; } = string.Empty;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerProfileData))]
internal sealed partial class ServerProfileDataJsonContext : JsonSerializerContext
{
}
