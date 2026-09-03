using ArkKeeper.Core.Ini;
using ArkKeeper.Core.Utils;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArkKeeper.Core.Profiles;

/// <summary>
/// A single ARK dedicated server configuration.
///
/// This starts with a representative subset of GameUserSettings.ini / Game.ini
/// settings (network identity, rules, difficulty, the core multipliers) rather
/// than the ~300+ settings the original tool exposed. The <see cref="IniSettingAttribute"/>
/// architecture is what needs to scale, not this specific property list — more
/// settings are added incrementally as their own tracked follow-ups.
/// </summary>
public sealed partial class ServerProfile : ObservableObject
{
    public ServerProfile()
    {
        ServerPassword = PasswordGenerator.Generate(16);
        AdminPassword = PasswordGenerator.Generate(16);
    }

    /// <summary>Local identity, not written to any game .ini file.</summary>
    public Guid ProfileId { get; init; } = Guid.NewGuid();

    /// <summary>Local display name for this profile, independent of the in-game session name.</summary>
    [ObservableProperty]
    private string _profileName = "New Server";

    #region Session / network (GameUserSettings.ini [SessionSettings])

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "SessionSettings", "SessionName")]
    private string _sessionName = "My ArkKeeper Server";

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "SessionSettings", "Port")]
    private int _port = 7777;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "SessionSettings", "QueryPort")]
    private int _queryPort = 27015;

    #endregion

    #region Passwords / RCON (GameUserSettings.ini [ServerSettings])

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ServerPassword")]
    private string _serverPassword = string.Empty;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ServerAdminPassword")]
    private string _adminPassword = string.Empty;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "RCONEnabled")]
    private bool _rconEnabled = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "RCONPort")]
    private int _rconPort = 27020;

    #endregion

    #region Rules (GameUserSettings.ini [ServerSettings])

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ServerPVE")]
    private bool _pveMode;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ServerHardcore")]
    private bool _hardcore;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ServerCrosshair")]
    private bool _showCrosshair = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ShowMapPlayerLocation")]
    private bool _showMapPlayerLocation = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AllowThirdPersonPlayer")]
    private bool _allowThirdPerson = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DisableStructureDecayPvE")]
    private bool _disableStructureDecayPve;

    #endregion

    #region Difficulty & multipliers (GameUserSettings.ini [ServerSettings])

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DifficultyOffset")]
    private float _difficultyOffset = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "XPMultiplier")]
    private float _xpMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TamingSpeedMultiplier")]
    private float _tamingSpeedMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "HarvestAmountMultiplier")]
    private float _harvestAmountMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ResourcesRespawnPeriodMultiplier")]
    private float _resourcesRespawnPeriodMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DayCycleSpeedScale")]
    private float _dayCycleSpeedScale = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DinoDamageMultiplier")]
    private float _dinoDamageMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PlayerDamageMultiplier")]
    private float _playerDamageMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "StructureDamageMultiplier")]
    private float _structureDamageMultiplier = 1.0f;

    #endregion

    #region Game.ini [/Script/Engine.GameSession]

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/Script/Engine.GameSession", "MaxPlayers")]
    private int _maxPlayers = 70;

    #endregion

    /// <summary>Applies values found in <paramref name="gameUserSettings"/> and <paramref name="game"/> onto this profile.</summary>
    public void ImportFrom(IniDocument gameUserSettings, IniDocument game)
    {
        IniSerializer.Apply(this, IniFile.GameUserSettings, gameUserSettings);
        IniSerializer.Apply(this, IniFile.Game, game);
    }

    /// <summary>Produces the GameUserSettings.ini contents for this profile.</summary>
    public IniDocument ToGameUserSettings() => IniSerializer.Write(this, IniFile.GameUserSettings);

    /// <summary>Produces the Game.ini contents for this profile.</summary>
    public IniDocument ToGameIni() => IniSerializer.Write(this, IniFile.Game);
}
