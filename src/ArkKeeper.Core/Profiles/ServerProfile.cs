using System.Collections.ObjectModel;
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

    #region GameUserSettings.ini [/Script/Engine.GameSession]

    // Corrected from IniFile.Game: the original tool's source (ArkData/ServerProfile.cs)
    // writes MaxPlayers to GameUserSettings.ini, not Game.ini — same section name, wrong file.
    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "/Script/Engine.GameSession", "MaxPlayers")]
    private int _maxPlayers = 70;

    #endregion

    #region Extended settings — GameUserSettings.ini [ServerSettings] (ported from the original tool)

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "SpectatorPassword")]
    private string _spectatorPassword = "";

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "BanListURL")]
    private string _banListURL = "http://arkdedicated.com/banlist.txt";

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "KickIdlePlayersPeriod")]
    private int _kickIdlePlayersPeriod = 3600;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "RCONServerGameLogBuffer")]
    private int _rCONServerGameLogBuffer = 600;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AdminLogging")]
    private bool _adminLogging = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ActiveMods")]
    private string _serverModIds = "";

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ExtinctionEventTimeInterval")]
    private int _extinctionEventTimeInterval = 2592000;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AutoSavePeriodMinutes")]
    private float _autoSavePeriodMinutes = 15.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ServerAutoForceRespawnWildDinosInterval")]
    private int _serverAutoForceRespawnWildDinosInterval = 86400;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TribeLogDestroyedEnemyStructures")]
    private bool _tribeLogDestroyedEnemyStructures = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AllowHideDamageSourceFromLogs")]
    private bool _allowHideDamageSourceFromLogs = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AllowCaveBuildingPvE")]
    private bool _allowCaveBuildingPvE = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "EnableExtraStructurePreventionVolumes")]
    private bool _enableExtraStructurePreventionVolumes = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "OverrideOfficialDifficulty")]
    private float _overrideOfficialDifficulty = 4.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "NoTributeDownloads")]
    private bool _enableTributeDownloads = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventDownloadSurvivors")]
    private bool _preventDownloadSurvivors = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventDownloadItems")]
    private bool _preventDownloadItems = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventDownloadDinos")]
    private bool _preventDownloadDinos = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventUploadSurvivors")]
    private bool _preventUploadSurvivors = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventUploadItems")]
    private bool _preventUploadItems = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventUploadDinos")]
    private bool _preventUploadDinos = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TributeCharacterExpirationSeconds")]
    private int _tributeCharacterExpirationSeconds = 86400;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TributeItemExpirationSeconds")]
    private int _tributeItemExpirationSeconds = 86400;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TributeDinoExpirationSeconds")]
    private int _tributeDinoExpirationSeconds = 86400;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "MinimumDinoReuploadInterval")]
    private int _minimumDinoReuploadInterval = 43200;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "CrossARKAllowForeignDinoDownloads")]
    private bool _crossARKAllowForeignDinoDownloads = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventOfflinePvP")]
    private bool _preventOfflinePvP = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventOfflinePvPInterval")]
    private int _preventOfflinePvPInterval = 900;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventTribeAlliances")]
    private bool _allowTribeAlliances = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventDiseases")]
    private bool _enableDiseases = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "NonPermanentDiseases")]
    private bool _nonPermanentDiseases = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "NPCNetworkStasisRangeScalePlayerCountStart")]
    private int _nPCNetworkStasisRangeScalePlayerCountStart = 70;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "NPCNetworkStasisRangeScalePlayerCountEnd")]
    private int _nPCNetworkStasisRangeScalePlayerCountEnd = 120;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "NPCNetworkStasisRangeScalePercentEnd")]
    private float _nPCNetworkStasisRangeScalePercentEnd = 0.5f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventSpawnAnimations")]
    private bool _preventSpawnAnimations = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "OxygenSwimSpeedStatMultiplier")]
    private float _oxygenSwimSpeedStatMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TribeNameChangeCooldown")]
    private int _tribeNameChangeCooldown = 15;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "globalVoiceChat")]
    private bool _enableGlobalVoiceChat = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "proximityChat")]
    private bool _enableProximityChat = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "alwaysNotifyPlayerLeft")]
    private bool _enablePlayerLeaveNotifications = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "alwaysNotifyPlayerJoined")]
    private bool _enablePlayerJoinedNotifications = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ServerForceNoHud")]
    private bool _allowHUD = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "EnablePVPGamma")]
    private bool _allowPVPGamma = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DisablePvEGamma")]
    private bool _allowPvEGamma = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ShowFloatingDamageText")]
    private bool _showFloatingDamageText = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AllowHitMarkers")]
    private bool _allowHitMarkers = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AllowFlyerCarryPVE")]
    private bool _enableFlyerCarry = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PlayerResistanceMultiplier")]
    private float _playerResistanceMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PlayerCharacterWaterDrainMultiplier")]
    private float _playerCharacterWaterDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PlayerCharacterFoodDrainMultiplier")]
    private float _playerCharacterFoodDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PlayerCharacterStaminaDrainMultiplier")]
    private float _playerCharacterStaminaDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PlayerCharacterHealthRecoveryMultiplier")]
    private float _playerCharacterHealthRecoveryMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TamedDinoDamageMultiplier")]
    private float _tamedDinoDamageMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DinoResistanceMultiplier")]
    private float _dinoResistanceMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TamedDinoResistanceMultiplier")]
    private float _tamedDinoResistanceMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "MaxTamedDinos")]
    private int _maxTamedDinos = 4000;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DinoCharacterFoodDrainMultiplier")]
    private float _dinoCharacterFoodDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DinoCharacterStaminaDrainMultiplier")]
    private float _dinoCharacterStaminaDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DinoCharacterHealthRecoveryMultiplier")]
    private float _dinoCharacterHealthRecoveryMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DinoCountMultiplier")]
    private float _dinoCountMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AllowRaidDinoFeeding")]
    private bool _allowRaidDinoFeeding = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "RaidDinoCharacterFoodDrainMultiplier")]
    private float _raidDinoCharacterFoodDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AllowFlyingStaminaRecovery")]
    private bool _allowFlyingStaminaRecovery = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PreventMateBoost")]
    private bool _preventMateBoost = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DisableDinoDecayPvE")]
    private bool _disableDinoDecayPvE = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PvPDinoDecay")]
    private bool _disableDinoDecayPvP = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AutoDestroyDecayedDinos")]
    private bool _autoDestroyDecayedDinos = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PvEDinoDecayPeriodMultiplier")]
    private float _pvEDinoDecayPeriodMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AllowMultipleAttachedC4")]
    private bool _allowMultipleAttachedC4 = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "MaxPersonalTamedDinos")]
    private float _maxPersonalTamedDinos = 40.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PersonalTamedDinosSaddleStructureCost")]
    private int _personalTamedDinosSaddleStructureCost = 19;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DisableImprintDinoBuff")]
    private bool _disableImprintDinoBuff = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AllowAnyoneBabyImprintCuddle")]
    private bool _allowAnyoneBabyImprintCuddle = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "HarvestHealthMultiplier")]
    private float _harvestHealthMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "UseOptimizedHarvestingHealth")]
    private bool _useOptimizedHarvestingHealth = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ClampResourceHarvestDamage")]
    private bool _clampResourceHarvestDamage = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ClampItemSpoilingTimes")]
    private bool _clampItemSpoilingTimes = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DayTimeSpeedScale")]
    private float _dayTimeSpeedScale = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "NightTimeSpeedScale")]
    private float _nightTimeSpeedScale = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DisableWeatherFog")]
    private bool _disableWeatherFog = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "StructureResistanceMultiplier")]
    private float _structureResistanceMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PvPStructureDecay")]
    private bool _pvPStructureDecay = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TheMaxStructuresInRange")]
    private float _maxStructuresVisible = 10500f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PerPlatformMaxStructuresMultiplier")]
    private float _perPlatformMaxStructuresMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "MaxPlatformSaddleStructureLimit")]
    private int _maxPlatformSaddleStructureLimit = 50;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "OverrideStructurePlatformPrevention")]
    private bool _overrideStructurePlatformPrevention = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PvEAllowStructuresAtSupplyDrops")]
    private bool _pvEAllowStructuresAtSupplyDrops = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DisableStructureDecayPVE")]
    private bool _enableStructureDecayPvE = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PvEStructureDecayDestructionPeriod")]
    private float _pvEStructureDecayDestructionPeriod = 0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "PvEStructureDecayPeriodMultiplier")]
    private float _pvEStructureDecayPeriodMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "AutoDestroyOldStructuresMultiplier")]
    private float _autoDestroyOldStructuresMultiplier = 0.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "ForceAllStructureLocking")]
    private bool _forceAllStructureLocking = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "OnlyAutoDestroyCoreStructures")]
    private bool _onlyAutoDestroyCoreStructures = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "OnlyDecayUnsnappedCoreStructures")]
    private bool _onlyDecayUnsnappedCoreStructures = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "FastDecayUnsnappedCoreStructures")]
    private bool _fastDecayUnsnappedCoreStructures = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "DestroyUnconnectedWaterPipes")]
    private bool _destroyUnconnectedWaterPipes = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "MaxNumberOfPlayersInTribe")]
    private int _sOTF_MaxNumberOfPlayersInTribe = 2;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "BattleNumOfTribesToStartGame")]
    private int _sOTF_BattleNumOfTribesToStartGame = 15;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "TimeToCollapseROD")]
    private int _sOTF_TimeToCollapseROD = 9000;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "BattleAutoStartGameInterval")]
    private int _sOTF_BattleAutoStartGameInterval = 60;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "BattleAutoRestartGameInterval")]
    private int _sOTF_BattleAutoRestartGameInterval = 45;

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "ServerSettings", "BattleSuddenDeathInterval")]
    private int _sOTF_BattleSuddenDeathInterval = 300;

    #endregion

    #region Extended settings — Game.ini [/script/shootergame.shootergamemode]

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "NextExtinctionEventUTC")]
    private int _extinctionEventUTC = 0;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "MaxTribeLogs")]
    private int _maxTribeLogs = 100;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bDisableFriendlyFire")]
    private bool _disableFriendlyFirePvP = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bPvEDisableFriendlyFire")]
    private bool _disableFriendlyFirePvE = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bDisableLootCrates")]
    private bool _disableLootCrates = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "MaxNumberOfPlayersInTribe")]
    private int _maxNumberOfPlayersInTribe = 70;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bIncreasePvPRespawnInterval")]
    private bool _increasePvPRespawnInterval = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "IncreasePvPRespawnIntervalCheckPeriod")]
    private int _increasePvPRespawnIntervalCheckPeriod = 300;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "IncreasePvPRespawnIntervalMultiplier")]
    private float _increasePvPRespawnIntervalMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "IncreasePvPRespawnIntervalBaseAmount")]
    private int _increasePvPRespawnIntervalBaseAmount = 60;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "PreventOfflinePvPConnectionInvincibleInterval")]
    private int _preventOfflinePvPConnectionInvincibleInterval = 5;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bAutoPvETimer")]
    private bool _autoPvETimer = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bAutoPvEUseSystemTime")]
    private bool _autoPvEUseSystemTime = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "AutoPvEStartTimeSeconds")]
    private int _autoPvEStartTimeSeconds = 0;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "AutoPvEStopTimeSeconds")]
    private int _autoPvEStopTimeSeconds = 0;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bPvEAllowTribeWar")]
    private bool _allowTribeWarPvE = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bPvEAllowTribeWarCancel")]
    private bool _allowTribeWarCancelPvE = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "MaxAlliancesPerTribe")]
    private int _maxAlliancesPerTribe = 10;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "MaxTribesPerAlliance")]
    private int _maxTribesPerAlliance = 10;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bAllowCustomRecipes")]
    private bool _allowCustomRecipes = true;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "CustomRecipeEffectivenessMultiplier")]
    private float _customRecipeEffectivenessMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "CustomRecipeSkillMultiplier")]
    private float _customRecipeSkillMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bUseCorpseLocator")]
    private bool _useCorpseLocator = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bAllowUnlimitedRespecs")]
    private bool _allowUnlimitedRespecs = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bAllowPlatformSaddleMultiFloors")]
    private bool _allowPlatformSaddleMultiFloors = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "SupplyCrateLootQualityMultiplier")]
    private float _supplyCrateLootQualityMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "FishingLootQualityMultiplier")]
    private float _fishingLootQualityMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "UseCorpseLifeSpanMultiplier")]
    private float _useCorpseLifeSpanMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "GlobalPoweredBatteryDurabilityDecreasePerSecond")]
    private float _globalPoweredBatteryDurabilityDecreasePerSecond = 4.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "RandomSupplyCratePoints")]
    private bool _randomSupplyCratePoints = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "OverrideMaxExperiencePointsPlayer")]
    private int _overrideMaxExperiencePointsPlayer = 0;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "PlayerHarvestingDamageMultiplier")]
    private float _playerHarvestingDamageMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "CraftingSkillBonusMultiplier")]
    private float _craftingSkillBonusMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "OverrideMaxExperiencePointsDino")]
    private int _overrideMaxExperiencePointsDino = 0;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "DinoHarvestingDamageMultiplier")]
    private float _dinoHarvestingDamageMultiplier = 3.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "DinoTurretDamageMultiplier")]
    private float _dinoTurretDamageMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bDisableDinoRiding")]
    private bool _disableDinoRiding = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bDisableDinoTaming")]
    private bool _disableDinoTaming = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bUseTameLimitForStructuresOnly")]
    private bool _useTameLimitForStructuresOnly = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "MatingIntervalMultiplier")]
    private float _matingIntervalMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "EggHatchSpeedMultiplier")]
    private float _eggHatchSpeedMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "BabyMatureSpeedMultiplier")]
    private float _babyMatureSpeedMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "BabyFoodConsumptionSpeedMultiplier")]
    private float _babyFoodConsumptionSpeedMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "BabyImprintingStatScaleMultiplier")]
    private float _babyImprintingStatScaleMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "BabyCuddleIntervalMultiplier")]
    private float _babyCuddleIntervalMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "BabyCuddleGracePeriodMultiplier")]
    private float _babyCuddleGracePeriodMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "BabyCuddleLoseImprintQualitySpeedMultiplier")]
    private float _babyCuddleLoseImprintQualitySpeedMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "WildDinoCharacterFoodDrainMultiplier")]
    private float _wildDinoCharacterFoodDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "TamedDinoCharacterFoodDrainMultiplier")]
    private float _tamedDinoCharacterFoodDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "WildDinoTorporDrainMultiplier")]
    private float _wildDinoTorporDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "TamedDinoTorporDrainMultiplier")]
    private float _tamedDinoTorporDrainMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "PassiveTameIntervalMultiplier")]
    private float _passiveTameIntervalMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "ResourceNoReplenishRadiusPlayers")]
    private float _resourceNoReplenishRadiusPlayers = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "ResourceNoReplenishRadiusStructures")]
    private float _resourceNoReplenishRadiusStructures = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "BaseTemperatureMultiplier")]
    private float _baseTemperatureMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "GlobalSpoilingTimeMultiplier")]
    private float _globalSpoilingTimeMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "GlobalCorpseDecompositionTimeMultiplier")]
    private float _globalCorpseDecompositionTimeMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "GlobalItemDecompositionTimeMultiplier")]
    private float _globalItemDecompositionTimeMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "CropDecaySpeedMultiplier")]
    private float _cropDecaySpeedMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "CropGrowthSpeedMultiplier")]
    private float _cropGrowthSpeedMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "LayEggIntervalMultiplier")]
    private float _layEggIntervalMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "PoopIntervalMultiplier")]
    private float _poopIntervalMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "HairGrowthSpeedMultiplier")]
    private float _hairGrowthSpeedMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "CraftXPMultiplier")]
    private float _craftXPMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "GenericXPMultiplier")]
    private float _genericXPMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "HarvestXPMultiplier")]
    private float _harvestXPMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "KillXPMultiplier")]
    private float _killXPMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "SpecialXPMultiplier")]
    private float _specialXPMultiplier = 1.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "StructureDamageRepairCooldown")]
    private int _structureDamageRepairCooldown = 180;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "PvPZoneStructureDamageMultiplier")]
    private float _pvPZoneStructureDamageMultiplier = 6.0f;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bFlyerPlatformAllowUnalignedDinoBasing")]
    private bool _flyerPlatformAllowUnalignedDinoBasing = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bPassiveDefensesDamageRiderlessDinos")]
    private bool _passiveDefensesDamageRiderlessDinos = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bDisableStructurePlacementCollision")]
    private bool _disableStructurePlacementCollision = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "FastDecayInterval")]
    private int _fastDecayInterval = 43200;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bLimitTurretsInRange")]
    private bool _limitTurretsInRange = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "LimitTurretsRange")]
    private int _limitTurretsRange = 10000;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "LimitTurretsNum")]
    private int _limitTurretsNum = 100;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bHardLimitTurretsInRange")]
    private bool _hardLimitTurretsInRange = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bOnlyAllowSpecifiedEngrams")]
    private bool _onlyAllowSpecifiedEngrams = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "bAutoUnlockAllEngrams")]
    private bool _autoUnlockAllEngrams = false;

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "PGMapName")]
    private string _pGM_Name = "";

    #endregion

    #region Extended settings — misc sections (MessageOfTheDay, MultiHome)

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "SessionSettings", "MultiHome")]
    private string _serverIP = "";

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "MessageOfTheDay", "Message")]
    private string _mOTD = "";

    [ObservableProperty]
    [property: IniSetting(IniFile.GameUserSettings, "MessageOfTheDay", "Duration")]
    private int _mOTDDuration = 20;

    #endregion

    #region Extended settings — Game.ini override lists (one raw value per repeated key)
    //
    // These are the "override list" settings from the original tool (per-dino-class damage/
    // resistance multipliers, engram overrides, supply crate loot, NPC spawn overrides...).
    // Each entry is kept as the whole "(ClassName=...,Multiplier=...)"-shaped ini value text
    // verbatim rather than parsed into named fields — that's enough to round-trip correctly
    // through the real game files (see IniSerializer's doc comment) without ArkKeeper needing a
    // typed model for every override struct's exact shape. A UI for editing these would still
    // need to either accept raw text or grow real per-type editors later.

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "DinoSpawnWeightMultipliers")]
    private List<string> _dinoSpawnWeightMultipliers = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "TamedDinoClassDamageMultipliers")]
    private List<string> _tamedDinoClassDamageMultipliers = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "TamedDinoClassResistanceMultipliers")]
    private List<string> _tamedDinoClassResistanceMultipliers = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "DinoClassDamageMultipliers")]
    private List<string> _dinoClassDamageMultipliers = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "DinoClassResistanceMultipliers")]
    private List<string> _dinoClassResistanceMultipliers = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "NPCReplacements")]
    private List<string> _nPCReplacements = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "PreventDinoTameClassNames")]
    private List<string> _preventDinoTameClassNames = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "HarvestResourceItemAmountClassMultipliers")]
    private List<string> _harvestResourceItemAmountClassMultipliers = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "OverrideNamedEngramEntries")]
    private List<string> _overrideNamedEngramEntries = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "ConfigOverrideItemCraftingCosts")]
    private List<string> _configOverrideItemCraftingCosts = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "ConfigAddNPCSpawnEntriesContainer")]
    private List<string> _configAddNPCSpawnEntriesContainer = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "ConfigSubtractNPCSpawnEntriesContainer")]
    private List<string> _configSubtractNPCSpawnEntriesContainer = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "ConfigOverrideNPCSpawnEntriesContainer")]
    private List<string> _configOverrideNPCSpawnEntriesContainer = new();

    [ObservableProperty]
    [property: IniSetting(IniFile.Game, "/script/shootergame.shootergamemode", "ConfigOverrideSupplyCrateItems")]
    private List<string> _configOverrideSupplyCrateItems = new();

    #endregion

    #region Launch-only settings (not written to any .ini file)

    /// <summary>ARK map identifier (e.g. "TheIsland", "Ragnarok"), passed on the launch command line.</summary>
    [ObservableProperty]
    private string _mapName = "TheIsland";

    /// <summary>Steam Workshop mod ids, in load order, passed via -mods= on the launch command line.</summary>
    public ObservableCollection<string> ModIds { get; init; } = new();

    /// <summary>Where SteamCMD installs (and the dedicated server runs from). Empty until the
    /// server files have been installed for this profile.</summary>
    [ObservableProperty]
    private string _installDirectory = string.Empty;

    #endregion

    /// <summary>Full path to the Windows dedicated server executable under <see cref="InstallDirectory"/>,
    /// as reported by Steam's own app manifest for app 376030 (ARK: Survival Evolved Dedicated Server).</summary>
    public string GetServerExecutablePath() =>
        Path.Combine(InstallDirectory, "ShooterGame", "Binaries", "Win64", "ShooterGameServer.exe");

    /// <summary>Directory holding this server's world saves, tribe/player profiles, etc.
    /// (ShooterGame\Saved\SavedArks — verified against the original tool's own Config.settings
    /// default, not guessed) — what <see cref="Snapshots.WorldBackupService"/> and
    /// <see cref="Saves.TribeFileReader"/> operate on for this profile.</summary>
    public string GetSaveDirectory() =>
        Path.Combine(InstallDirectory, "ShooterGame", "Saved", "SavedArks");

    /// <summary>Directory the dedicated server reads GameUserSettings.ini/Game.ini from on
    /// Windows (ShooterGame\Saved\Config\WindowsServer — the standard ARK dedicated server
    /// convention, same as the original tool).</summary>
    public string GetConfigDirectory() =>
        Path.Combine(InstallDirectory, "ShooterGame", "Saved", "Config", "WindowsServer");

    /// <summary>Writes GameUserSettings.ini/Game.ini into <see cref="GetConfigDirectory"/> so the
    /// dedicated server actually picks up these settings on its next start. Merges onto whatever
    /// ini content is already there (parsed first, if the file exists) rather than overwriting
    /// wholesale — ARK has far more settings than this profile models, and a mod or a manual edit
    /// may have added directives here that a blind overwrite would silently destroy.</summary>
    public void WriteConfigFiles()
    {
        if (string.IsNullOrWhiteSpace(InstallDirectory))
        {
            // Nothing to write to yet — Start() will fail right after with a clearer,
            // specific "server executable not found" error instead.
            return;
        }

        ServerModIds = string.Join(',', ModIds);

        var configDirectory = GetConfigDirectory();
        Directory.CreateDirectory(configDirectory);

        WriteMergedIniFile(Path.Combine(configDirectory, "GameUserSettings.ini"), IniFile.GameUserSettings);
        WriteMergedIniFile(Path.Combine(configDirectory, "Game.ini"), IniFile.Game);
    }

    private void WriteMergedIniFile(string path, IniFile file)
    {
        var document = File.Exists(path) ? IniDocument.Parse(File.ReadAllText(path)) : new IniDocument();
        IniSerializer.Write(this, file, document);
        File.WriteAllText(path, document.ToString());
    }

    /// <summary>Applies values found in <paramref name="gameUserSettings"/> and <paramref name="game"/> onto this profile.</summary>
    public void ImportFrom(IniDocument gameUserSettings, IniDocument game)
    {
        IniSerializer.Apply(this, IniFile.GameUserSettings, gameUserSettings);
        IniSerializer.Apply(this, IniFile.Game, game);

        // ServerModIds (the ActiveMods ini key) just got overwritten by the Apply call above —
        // mirror it into the rich ModIds collection, which is what a mods UI would actually
        // read/edit (LaunchArgumentsBuilder uses ModIds for -mods=, not this raw string).
        ModIds.Clear();
        foreach (var modId in ServerModIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            ModIds.Add(modId);
        }
    }

    /// <summary>Produces the GameUserSettings.ini contents for this profile. Syncs
    /// <see cref="ServerModIds"/> (the ActiveMods ini key) from <see cref="ModIds"/> first — the
    /// two aren't kept in sync automatically as <see cref="ModIds"/> changes, since the ini value
    /// only actually matters at write time.</summary>
    public IniDocument ToGameUserSettings()
    {
        ServerModIds = string.Join(',', ModIds);
        return IniSerializer.Write(this, IniFile.GameUserSettings);
    }

    /// <summary>Produces the Game.ini contents for this profile.</summary>
    public IniDocument ToGameIni() => IniSerializer.Write(this, IniFile.Game);

    /// <summary>Snapshots this profile into a plain, JSON-source-gen-friendly <see cref="ServerProfileData"/>
    /// (see that type's doc comment for why this indirection exists).</summary>
    public ServerProfileData ToData() => new()
    {
        ProfileId = ProfileId,
        ProfileName = ProfileName,
        SessionName = SessionName,
        Port = Port,
        QueryPort = QueryPort,
        ServerPassword = ServerPassword,
        AdminPassword = AdminPassword,
        RconEnabled = RconEnabled,
        RconPort = RconPort,
        PveMode = PveMode,
        Hardcore = Hardcore,
        ShowCrosshair = ShowCrosshair,
        ShowMapPlayerLocation = ShowMapPlayerLocation,
        AllowThirdPerson = AllowThirdPerson,
        DisableStructureDecayPve = DisableStructureDecayPve,
        DifficultyOffset = DifficultyOffset,
        XpMultiplier = XpMultiplier,
        TamingSpeedMultiplier = TamingSpeedMultiplier,
        HarvestAmountMultiplier = HarvestAmountMultiplier,
        ResourcesRespawnPeriodMultiplier = ResourcesRespawnPeriodMultiplier,
        DayCycleSpeedScale = DayCycleSpeedScale,
        DinoDamageMultiplier = DinoDamageMultiplier,
        PlayerDamageMultiplier = PlayerDamageMultiplier,
        StructureDamageMultiplier = StructureDamageMultiplier,
        MaxPlayers = MaxPlayers,
        MapName = MapName,
        ModIds = new List<string>(ModIds),
        InstallDirectory = InstallDirectory,

        SpectatorPassword = SpectatorPassword,
        ServerIP = ServerIP,
        BanListURL = BanListURL,
        KickIdlePlayersPeriod = KickIdlePlayersPeriod,
        RCONServerGameLogBuffer = RCONServerGameLogBuffer,
        AdminLogging = AdminLogging,
        ServerModIds = ServerModIds,
        ExtinctionEventTimeInterval = ExtinctionEventTimeInterval,
        ExtinctionEventUTC = ExtinctionEventUTC,
        AutoSavePeriodMinutes = AutoSavePeriodMinutes,
        MOTD = MOTD,
        MOTDDuration = MOTDDuration,
        ServerAutoForceRespawnWildDinosInterval = ServerAutoForceRespawnWildDinosInterval,
        MaxTribeLogs = MaxTribeLogs,
        TribeLogDestroyedEnemyStructures = TribeLogDestroyedEnemyStructures,
        AllowHideDamageSourceFromLogs = AllowHideDamageSourceFromLogs,
        AllowCaveBuildingPvE = AllowCaveBuildingPvE,
        DisableFriendlyFirePvP = DisableFriendlyFirePvP,
        DisableFriendlyFirePvE = DisableFriendlyFirePvE,
        DisableLootCrates = DisableLootCrates,
        EnableExtraStructurePreventionVolumes = EnableExtraStructurePreventionVolumes,
        OverrideOfficialDifficulty = OverrideOfficialDifficulty,
        MaxNumberOfPlayersInTribe = MaxNumberOfPlayersInTribe,
        EnableTributeDownloads = EnableTributeDownloads,
        PreventDownloadSurvivors = PreventDownloadSurvivors,
        PreventDownloadItems = PreventDownloadItems,
        PreventDownloadDinos = PreventDownloadDinos,
        PreventUploadSurvivors = PreventUploadSurvivors,
        PreventUploadItems = PreventUploadItems,
        PreventUploadDinos = PreventUploadDinos,
        TributeCharacterExpirationSeconds = TributeCharacterExpirationSeconds,
        TributeItemExpirationSeconds = TributeItemExpirationSeconds,
        TributeDinoExpirationSeconds = TributeDinoExpirationSeconds,
        MinimumDinoReuploadInterval = MinimumDinoReuploadInterval,
        CrossARKAllowForeignDinoDownloads = CrossARKAllowForeignDinoDownloads,
        IncreasePvPRespawnInterval = IncreasePvPRespawnInterval,
        IncreasePvPRespawnIntervalCheckPeriod = IncreasePvPRespawnIntervalCheckPeriod,
        IncreasePvPRespawnIntervalMultiplier = IncreasePvPRespawnIntervalMultiplier,
        IncreasePvPRespawnIntervalBaseAmount = IncreasePvPRespawnIntervalBaseAmount,
        PreventOfflinePvP = PreventOfflinePvP,
        PreventOfflinePvPInterval = PreventOfflinePvPInterval,
        PreventOfflinePvPConnectionInvincibleInterval = PreventOfflinePvPConnectionInvincibleInterval,
        AutoPvETimer = AutoPvETimer,
        AutoPvEUseSystemTime = AutoPvEUseSystemTime,
        AutoPvEStartTimeSeconds = AutoPvEStartTimeSeconds,
        AutoPvEStopTimeSeconds = AutoPvEStopTimeSeconds,
        AllowTribeWarPvE = AllowTribeWarPvE,
        AllowTribeWarCancelPvE = AllowTribeWarCancelPvE,
        AllowTribeAlliances = AllowTribeAlliances,
        MaxAlliancesPerTribe = MaxAlliancesPerTribe,
        MaxTribesPerAlliance = MaxTribesPerAlliance,
        AllowCustomRecipes = AllowCustomRecipes,
        CustomRecipeEffectivenessMultiplier = CustomRecipeEffectivenessMultiplier,
        CustomRecipeSkillMultiplier = CustomRecipeSkillMultiplier,
        EnableDiseases = EnableDiseases,
        NonPermanentDiseases = NonPermanentDiseases,
        NPCNetworkStasisRangeScalePlayerCountStart = NPCNetworkStasisRangeScalePlayerCountStart,
        NPCNetworkStasisRangeScalePlayerCountEnd = NPCNetworkStasisRangeScalePlayerCountEnd,
        NPCNetworkStasisRangeScalePercentEnd = NPCNetworkStasisRangeScalePercentEnd,
        UseCorpseLocator = UseCorpseLocator,
        PreventSpawnAnimations = PreventSpawnAnimations,
        AllowUnlimitedRespecs = AllowUnlimitedRespecs,
        AllowPlatformSaddleMultiFloors = AllowPlatformSaddleMultiFloors,
        OxygenSwimSpeedStatMultiplier = OxygenSwimSpeedStatMultiplier,
        SupplyCrateLootQualityMultiplier = SupplyCrateLootQualityMultiplier,
        FishingLootQualityMultiplier = FishingLootQualityMultiplier,
        UseCorpseLifeSpanMultiplier = UseCorpseLifeSpanMultiplier,
        GlobalPoweredBatteryDurabilityDecreasePerSecond = GlobalPoweredBatteryDurabilityDecreasePerSecond,
        TribeNameChangeCooldown = TribeNameChangeCooldown,
        RandomSupplyCratePoints = RandomSupplyCratePoints,
        EnableGlobalVoiceChat = EnableGlobalVoiceChat,
        EnableProximityChat = EnableProximityChat,
        EnablePlayerLeaveNotifications = EnablePlayerLeaveNotifications,
        EnablePlayerJoinedNotifications = EnablePlayerJoinedNotifications,
        AllowHUD = AllowHUD,
        AllowPVPGamma = AllowPVPGamma,
        AllowPvEGamma = AllowPvEGamma,
        ShowFloatingDamageText = ShowFloatingDamageText,
        AllowHitMarkers = AllowHitMarkers,
        EnableFlyerCarry = EnableFlyerCarry,
        OverrideMaxExperiencePointsPlayer = OverrideMaxExperiencePointsPlayer,
        PlayerResistanceMultiplier = PlayerResistanceMultiplier,
        PlayerCharacterWaterDrainMultiplier = PlayerCharacterWaterDrainMultiplier,
        PlayerCharacterFoodDrainMultiplier = PlayerCharacterFoodDrainMultiplier,
        PlayerCharacterStaminaDrainMultiplier = PlayerCharacterStaminaDrainMultiplier,
        PlayerCharacterHealthRecoveryMultiplier = PlayerCharacterHealthRecoveryMultiplier,
        PlayerHarvestingDamageMultiplier = PlayerHarvestingDamageMultiplier,
        CraftingSkillBonusMultiplier = CraftingSkillBonusMultiplier,
        OverrideMaxExperiencePointsDino = OverrideMaxExperiencePointsDino,
        TamedDinoDamageMultiplier = TamedDinoDamageMultiplier,
        DinoResistanceMultiplier = DinoResistanceMultiplier,
        TamedDinoResistanceMultiplier = TamedDinoResistanceMultiplier,
        MaxTamedDinos = MaxTamedDinos,
        DinoCharacterFoodDrainMultiplier = DinoCharacterFoodDrainMultiplier,
        DinoCharacterStaminaDrainMultiplier = DinoCharacterStaminaDrainMultiplier,
        DinoCharacterHealthRecoveryMultiplier = DinoCharacterHealthRecoveryMultiplier,
        DinoCountMultiplier = DinoCountMultiplier,
        DinoHarvestingDamageMultiplier = DinoHarvestingDamageMultiplier,
        DinoTurretDamageMultiplier = DinoTurretDamageMultiplier,
        AllowRaidDinoFeeding = AllowRaidDinoFeeding,
        RaidDinoCharacterFoodDrainMultiplier = RaidDinoCharacterFoodDrainMultiplier,
        AllowFlyingStaminaRecovery = AllowFlyingStaminaRecovery,
        PreventMateBoost = PreventMateBoost,
        DisableDinoDecayPvE = DisableDinoDecayPvE,
        DisableDinoDecayPvP = DisableDinoDecayPvP,
        AutoDestroyDecayedDinos = AutoDestroyDecayedDinos,
        PvEDinoDecayPeriodMultiplier = PvEDinoDecayPeriodMultiplier,
        AllowMultipleAttachedC4 = AllowMultipleAttachedC4,
        DisableDinoRiding = DisableDinoRiding,
        DisableDinoTaming = DisableDinoTaming,
        MaxPersonalTamedDinos = MaxPersonalTamedDinos,
        PersonalTamedDinosSaddleStructureCost = PersonalTamedDinosSaddleStructureCost,
        UseTameLimitForStructuresOnly = UseTameLimitForStructuresOnly,
        MatingIntervalMultiplier = MatingIntervalMultiplier,
        EggHatchSpeedMultiplier = EggHatchSpeedMultiplier,
        BabyMatureSpeedMultiplier = BabyMatureSpeedMultiplier,
        BabyFoodConsumptionSpeedMultiplier = BabyFoodConsumptionSpeedMultiplier,
        DisableImprintDinoBuff = DisableImprintDinoBuff,
        AllowAnyoneBabyImprintCuddle = AllowAnyoneBabyImprintCuddle,
        BabyImprintingStatScaleMultiplier = BabyImprintingStatScaleMultiplier,
        BabyCuddleIntervalMultiplier = BabyCuddleIntervalMultiplier,
        BabyCuddleGracePeriodMultiplier = BabyCuddleGracePeriodMultiplier,
        BabyCuddleLoseImprintQualitySpeedMultiplier = BabyCuddleLoseImprintQualitySpeedMultiplier,
        WildDinoCharacterFoodDrainMultiplier = WildDinoCharacterFoodDrainMultiplier,
        TamedDinoCharacterFoodDrainMultiplier = TamedDinoCharacterFoodDrainMultiplier,
        WildDinoTorporDrainMultiplier = WildDinoTorporDrainMultiplier,
        TamedDinoTorporDrainMultiplier = TamedDinoTorporDrainMultiplier,
        PassiveTameIntervalMultiplier = PassiveTameIntervalMultiplier,
        ResourceNoReplenishRadiusPlayers = ResourceNoReplenishRadiusPlayers,
        ResourceNoReplenishRadiusStructures = ResourceNoReplenishRadiusStructures,
        HarvestHealthMultiplier = HarvestHealthMultiplier,
        UseOptimizedHarvestingHealth = UseOptimizedHarvestingHealth,
        ClampResourceHarvestDamage = ClampResourceHarvestDamage,
        ClampItemSpoilingTimes = ClampItemSpoilingTimes,
        BaseTemperatureMultiplier = BaseTemperatureMultiplier,
        DayTimeSpeedScale = DayTimeSpeedScale,
        NightTimeSpeedScale = NightTimeSpeedScale,
        GlobalSpoilingTimeMultiplier = GlobalSpoilingTimeMultiplier,
        GlobalCorpseDecompositionTimeMultiplier = GlobalCorpseDecompositionTimeMultiplier,
        GlobalItemDecompositionTimeMultiplier = GlobalItemDecompositionTimeMultiplier,
        CropDecaySpeedMultiplier = CropDecaySpeedMultiplier,
        CropGrowthSpeedMultiplier = CropGrowthSpeedMultiplier,
        LayEggIntervalMultiplier = LayEggIntervalMultiplier,
        PoopIntervalMultiplier = PoopIntervalMultiplier,
        HairGrowthSpeedMultiplier = HairGrowthSpeedMultiplier,
        CraftXPMultiplier = CraftXPMultiplier,
        GenericXPMultiplier = GenericXPMultiplier,
        HarvestXPMultiplier = HarvestXPMultiplier,
        KillXPMultiplier = KillXPMultiplier,
        SpecialXPMultiplier = SpecialXPMultiplier,
        DisableWeatherFog = DisableWeatherFog,
        StructureResistanceMultiplier = StructureResistanceMultiplier,
        StructureDamageRepairCooldown = StructureDamageRepairCooldown,
        PvPStructureDecay = PvPStructureDecay,
        PvPZoneStructureDamageMultiplier = PvPZoneStructureDamageMultiplier,
        MaxStructuresVisible = MaxStructuresVisible,
        PerPlatformMaxStructuresMultiplier = PerPlatformMaxStructuresMultiplier,
        MaxPlatformSaddleStructureLimit = MaxPlatformSaddleStructureLimit,
        OverrideStructurePlatformPrevention = OverrideStructurePlatformPrevention,
        FlyerPlatformAllowUnalignedDinoBasing = FlyerPlatformAllowUnalignedDinoBasing,
        PvEAllowStructuresAtSupplyDrops = PvEAllowStructuresAtSupplyDrops,
        EnableStructureDecayPvE = EnableStructureDecayPvE,
        PvEStructureDecayDestructionPeriod = PvEStructureDecayDestructionPeriod,
        PvEStructureDecayPeriodMultiplier = PvEStructureDecayPeriodMultiplier,
        AutoDestroyOldStructuresMultiplier = AutoDestroyOldStructuresMultiplier,
        ForceAllStructureLocking = ForceAllStructureLocking,
        PassiveDefensesDamageRiderlessDinos = PassiveDefensesDamageRiderlessDinos,
        OnlyAutoDestroyCoreStructures = OnlyAutoDestroyCoreStructures,
        OnlyDecayUnsnappedCoreStructures = OnlyDecayUnsnappedCoreStructures,
        FastDecayUnsnappedCoreStructures = FastDecayUnsnappedCoreStructures,
        DestroyUnconnectedWaterPipes = DestroyUnconnectedWaterPipes,
        DisableStructurePlacementCollision = DisableStructurePlacementCollision,
        FastDecayInterval = FastDecayInterval,
        LimitTurretsInRange = LimitTurretsInRange,
        LimitTurretsRange = LimitTurretsRange,
        LimitTurretsNum = LimitTurretsNum,
        HardLimitTurretsInRange = HardLimitTurretsInRange,
        OnlyAllowSpecifiedEngrams = OnlyAllowSpecifiedEngrams,
        AutoUnlockAllEngrams = AutoUnlockAllEngrams,
        PGM_Name = PGM_Name,
        SOTF_MaxNumberOfPlayersInTribe = SOTF_MaxNumberOfPlayersInTribe,
        SOTF_BattleNumOfTribesToStartGame = SOTF_BattleNumOfTribesToStartGame,
        SOTF_TimeToCollapseROD = SOTF_TimeToCollapseROD,
        SOTF_BattleAutoStartGameInterval = SOTF_BattleAutoStartGameInterval,
        SOTF_BattleAutoRestartGameInterval = SOTF_BattleAutoRestartGameInterval,
        SOTF_BattleSuddenDeathInterval = SOTF_BattleSuddenDeathInterval,

        DinoSpawnWeightMultipliers = new List<string>(DinoSpawnWeightMultipliers),
        TamedDinoClassDamageMultipliers = new List<string>(TamedDinoClassDamageMultipliers),
        TamedDinoClassResistanceMultipliers = new List<string>(TamedDinoClassResistanceMultipliers),
        DinoClassDamageMultipliers = new List<string>(DinoClassDamageMultipliers),
        DinoClassResistanceMultipliers = new List<string>(DinoClassResistanceMultipliers),
        NPCReplacements = new List<string>(NPCReplacements),
        PreventDinoTameClassNames = new List<string>(PreventDinoTameClassNames),
        HarvestResourceItemAmountClassMultipliers = new List<string>(HarvestResourceItemAmountClassMultipliers),
        OverrideNamedEngramEntries = new List<string>(OverrideNamedEngramEntries),
        ConfigOverrideItemCraftingCosts = new List<string>(ConfigOverrideItemCraftingCosts),
        ConfigAddNPCSpawnEntriesContainer = new List<string>(ConfigAddNPCSpawnEntriesContainer),
        ConfigSubtractNPCSpawnEntriesContainer = new List<string>(ConfigSubtractNPCSpawnEntriesContainer),
        ConfigOverrideNPCSpawnEntriesContainer = new List<string>(ConfigOverrideNPCSpawnEntriesContainer),
        ConfigOverrideSupplyCrateItems = new List<string>(ConfigOverrideSupplyCrateItems),
    };

    /// <summary>Rebuilds a <see cref="ServerProfile"/> from a snapshot produced by <see cref="ToData"/>.</summary>
    public static ServerProfile FromData(ServerProfileData data)
    {
        var profile = new ServerProfile
        {
            ProfileId = data.ProfileId,
            ProfileName = data.ProfileName,
            SessionName = data.SessionName,
            Port = data.Port,
            QueryPort = data.QueryPort,
            ServerPassword = data.ServerPassword,
            AdminPassword = data.AdminPassword,
            RconEnabled = data.RconEnabled,
            RconPort = data.RconPort,
            PveMode = data.PveMode,
            Hardcore = data.Hardcore,
            ShowCrosshair = data.ShowCrosshair,
            ShowMapPlayerLocation = data.ShowMapPlayerLocation,
            AllowThirdPerson = data.AllowThirdPerson,
            DisableStructureDecayPve = data.DisableStructureDecayPve,
            DifficultyOffset = data.DifficultyOffset,
            XpMultiplier = data.XpMultiplier,
            TamingSpeedMultiplier = data.TamingSpeedMultiplier,
            HarvestAmountMultiplier = data.HarvestAmountMultiplier,
            ResourcesRespawnPeriodMultiplier = data.ResourcesRespawnPeriodMultiplier,
            DayCycleSpeedScale = data.DayCycleSpeedScale,
            DinoDamageMultiplier = data.DinoDamageMultiplier,
            PlayerDamageMultiplier = data.PlayerDamageMultiplier,
            StructureDamageMultiplier = data.StructureDamageMultiplier,
            MaxPlayers = data.MaxPlayers,
            MapName = data.MapName,
            InstallDirectory = data.InstallDirectory,

            SpectatorPassword = data.SpectatorPassword,
            ServerIP = data.ServerIP,
            BanListURL = data.BanListURL,
            KickIdlePlayersPeriod = data.KickIdlePlayersPeriod,
            RCONServerGameLogBuffer = data.RCONServerGameLogBuffer,
            AdminLogging = data.AdminLogging,
            ServerModIds = data.ServerModIds,
            ExtinctionEventTimeInterval = data.ExtinctionEventTimeInterval,
            ExtinctionEventUTC = data.ExtinctionEventUTC,
            AutoSavePeriodMinutes = data.AutoSavePeriodMinutes,
            MOTD = data.MOTD,
            MOTDDuration = data.MOTDDuration,
            ServerAutoForceRespawnWildDinosInterval = data.ServerAutoForceRespawnWildDinosInterval,
            MaxTribeLogs = data.MaxTribeLogs,
            TribeLogDestroyedEnemyStructures = data.TribeLogDestroyedEnemyStructures,
            AllowHideDamageSourceFromLogs = data.AllowHideDamageSourceFromLogs,
            AllowCaveBuildingPvE = data.AllowCaveBuildingPvE,
            DisableFriendlyFirePvP = data.DisableFriendlyFirePvP,
            DisableFriendlyFirePvE = data.DisableFriendlyFirePvE,
            DisableLootCrates = data.DisableLootCrates,
            EnableExtraStructurePreventionVolumes = data.EnableExtraStructurePreventionVolumes,
            OverrideOfficialDifficulty = data.OverrideOfficialDifficulty,
            MaxNumberOfPlayersInTribe = data.MaxNumberOfPlayersInTribe,
            EnableTributeDownloads = data.EnableTributeDownloads,
            PreventDownloadSurvivors = data.PreventDownloadSurvivors,
            PreventDownloadItems = data.PreventDownloadItems,
            PreventDownloadDinos = data.PreventDownloadDinos,
            PreventUploadSurvivors = data.PreventUploadSurvivors,
            PreventUploadItems = data.PreventUploadItems,
            PreventUploadDinos = data.PreventUploadDinos,
            TributeCharacterExpirationSeconds = data.TributeCharacterExpirationSeconds,
            TributeItemExpirationSeconds = data.TributeItemExpirationSeconds,
            TributeDinoExpirationSeconds = data.TributeDinoExpirationSeconds,
            MinimumDinoReuploadInterval = data.MinimumDinoReuploadInterval,
            CrossARKAllowForeignDinoDownloads = data.CrossARKAllowForeignDinoDownloads,
            IncreasePvPRespawnInterval = data.IncreasePvPRespawnInterval,
            IncreasePvPRespawnIntervalCheckPeriod = data.IncreasePvPRespawnIntervalCheckPeriod,
            IncreasePvPRespawnIntervalMultiplier = data.IncreasePvPRespawnIntervalMultiplier,
            IncreasePvPRespawnIntervalBaseAmount = data.IncreasePvPRespawnIntervalBaseAmount,
            PreventOfflinePvP = data.PreventOfflinePvP,
            PreventOfflinePvPInterval = data.PreventOfflinePvPInterval,
            PreventOfflinePvPConnectionInvincibleInterval = data.PreventOfflinePvPConnectionInvincibleInterval,
            AutoPvETimer = data.AutoPvETimer,
            AutoPvEUseSystemTime = data.AutoPvEUseSystemTime,
            AutoPvEStartTimeSeconds = data.AutoPvEStartTimeSeconds,
            AutoPvEStopTimeSeconds = data.AutoPvEStopTimeSeconds,
            AllowTribeWarPvE = data.AllowTribeWarPvE,
            AllowTribeWarCancelPvE = data.AllowTribeWarCancelPvE,
            AllowTribeAlliances = data.AllowTribeAlliances,
            MaxAlliancesPerTribe = data.MaxAlliancesPerTribe,
            MaxTribesPerAlliance = data.MaxTribesPerAlliance,
            AllowCustomRecipes = data.AllowCustomRecipes,
            CustomRecipeEffectivenessMultiplier = data.CustomRecipeEffectivenessMultiplier,
            CustomRecipeSkillMultiplier = data.CustomRecipeSkillMultiplier,
            EnableDiseases = data.EnableDiseases,
            NonPermanentDiseases = data.NonPermanentDiseases,
            NPCNetworkStasisRangeScalePlayerCountStart = data.NPCNetworkStasisRangeScalePlayerCountStart,
            NPCNetworkStasisRangeScalePlayerCountEnd = data.NPCNetworkStasisRangeScalePlayerCountEnd,
            NPCNetworkStasisRangeScalePercentEnd = data.NPCNetworkStasisRangeScalePercentEnd,
            UseCorpseLocator = data.UseCorpseLocator,
            PreventSpawnAnimations = data.PreventSpawnAnimations,
            AllowUnlimitedRespecs = data.AllowUnlimitedRespecs,
            AllowPlatformSaddleMultiFloors = data.AllowPlatformSaddleMultiFloors,
            OxygenSwimSpeedStatMultiplier = data.OxygenSwimSpeedStatMultiplier,
            SupplyCrateLootQualityMultiplier = data.SupplyCrateLootQualityMultiplier,
            FishingLootQualityMultiplier = data.FishingLootQualityMultiplier,
            UseCorpseLifeSpanMultiplier = data.UseCorpseLifeSpanMultiplier,
            GlobalPoweredBatteryDurabilityDecreasePerSecond = data.GlobalPoweredBatteryDurabilityDecreasePerSecond,
            TribeNameChangeCooldown = data.TribeNameChangeCooldown,
            RandomSupplyCratePoints = data.RandomSupplyCratePoints,
            EnableGlobalVoiceChat = data.EnableGlobalVoiceChat,
            EnableProximityChat = data.EnableProximityChat,
            EnablePlayerLeaveNotifications = data.EnablePlayerLeaveNotifications,
            EnablePlayerJoinedNotifications = data.EnablePlayerJoinedNotifications,
            AllowHUD = data.AllowHUD,
            AllowPVPGamma = data.AllowPVPGamma,
            AllowPvEGamma = data.AllowPvEGamma,
            ShowFloatingDamageText = data.ShowFloatingDamageText,
            AllowHitMarkers = data.AllowHitMarkers,
            EnableFlyerCarry = data.EnableFlyerCarry,
            OverrideMaxExperiencePointsPlayer = data.OverrideMaxExperiencePointsPlayer,
            PlayerResistanceMultiplier = data.PlayerResistanceMultiplier,
            PlayerCharacterWaterDrainMultiplier = data.PlayerCharacterWaterDrainMultiplier,
            PlayerCharacterFoodDrainMultiplier = data.PlayerCharacterFoodDrainMultiplier,
            PlayerCharacterStaminaDrainMultiplier = data.PlayerCharacterStaminaDrainMultiplier,
            PlayerCharacterHealthRecoveryMultiplier = data.PlayerCharacterHealthRecoveryMultiplier,
            PlayerHarvestingDamageMultiplier = data.PlayerHarvestingDamageMultiplier,
            CraftingSkillBonusMultiplier = data.CraftingSkillBonusMultiplier,
            OverrideMaxExperiencePointsDino = data.OverrideMaxExperiencePointsDino,
            TamedDinoDamageMultiplier = data.TamedDinoDamageMultiplier,
            DinoResistanceMultiplier = data.DinoResistanceMultiplier,
            TamedDinoResistanceMultiplier = data.TamedDinoResistanceMultiplier,
            MaxTamedDinos = data.MaxTamedDinos,
            DinoCharacterFoodDrainMultiplier = data.DinoCharacterFoodDrainMultiplier,
            DinoCharacterStaminaDrainMultiplier = data.DinoCharacterStaminaDrainMultiplier,
            DinoCharacterHealthRecoveryMultiplier = data.DinoCharacterHealthRecoveryMultiplier,
            DinoCountMultiplier = data.DinoCountMultiplier,
            DinoHarvestingDamageMultiplier = data.DinoHarvestingDamageMultiplier,
            DinoTurretDamageMultiplier = data.DinoTurretDamageMultiplier,
            AllowRaidDinoFeeding = data.AllowRaidDinoFeeding,
            RaidDinoCharacterFoodDrainMultiplier = data.RaidDinoCharacterFoodDrainMultiplier,
            AllowFlyingStaminaRecovery = data.AllowFlyingStaminaRecovery,
            PreventMateBoost = data.PreventMateBoost,
            DisableDinoDecayPvE = data.DisableDinoDecayPvE,
            DisableDinoDecayPvP = data.DisableDinoDecayPvP,
            AutoDestroyDecayedDinos = data.AutoDestroyDecayedDinos,
            PvEDinoDecayPeriodMultiplier = data.PvEDinoDecayPeriodMultiplier,
            AllowMultipleAttachedC4 = data.AllowMultipleAttachedC4,
            DisableDinoRiding = data.DisableDinoRiding,
            DisableDinoTaming = data.DisableDinoTaming,
            MaxPersonalTamedDinos = data.MaxPersonalTamedDinos,
            PersonalTamedDinosSaddleStructureCost = data.PersonalTamedDinosSaddleStructureCost,
            UseTameLimitForStructuresOnly = data.UseTameLimitForStructuresOnly,
            MatingIntervalMultiplier = data.MatingIntervalMultiplier,
            EggHatchSpeedMultiplier = data.EggHatchSpeedMultiplier,
            BabyMatureSpeedMultiplier = data.BabyMatureSpeedMultiplier,
            BabyFoodConsumptionSpeedMultiplier = data.BabyFoodConsumptionSpeedMultiplier,
            DisableImprintDinoBuff = data.DisableImprintDinoBuff,
            AllowAnyoneBabyImprintCuddle = data.AllowAnyoneBabyImprintCuddle,
            BabyImprintingStatScaleMultiplier = data.BabyImprintingStatScaleMultiplier,
            BabyCuddleIntervalMultiplier = data.BabyCuddleIntervalMultiplier,
            BabyCuddleGracePeriodMultiplier = data.BabyCuddleGracePeriodMultiplier,
            BabyCuddleLoseImprintQualitySpeedMultiplier = data.BabyCuddleLoseImprintQualitySpeedMultiplier,
            WildDinoCharacterFoodDrainMultiplier = data.WildDinoCharacterFoodDrainMultiplier,
            TamedDinoCharacterFoodDrainMultiplier = data.TamedDinoCharacterFoodDrainMultiplier,
            WildDinoTorporDrainMultiplier = data.WildDinoTorporDrainMultiplier,
            TamedDinoTorporDrainMultiplier = data.TamedDinoTorporDrainMultiplier,
            PassiveTameIntervalMultiplier = data.PassiveTameIntervalMultiplier,
            ResourceNoReplenishRadiusPlayers = data.ResourceNoReplenishRadiusPlayers,
            ResourceNoReplenishRadiusStructures = data.ResourceNoReplenishRadiusStructures,
            HarvestHealthMultiplier = data.HarvestHealthMultiplier,
            UseOptimizedHarvestingHealth = data.UseOptimizedHarvestingHealth,
            ClampResourceHarvestDamage = data.ClampResourceHarvestDamage,
            ClampItemSpoilingTimes = data.ClampItemSpoilingTimes,
            BaseTemperatureMultiplier = data.BaseTemperatureMultiplier,
            DayTimeSpeedScale = data.DayTimeSpeedScale,
            NightTimeSpeedScale = data.NightTimeSpeedScale,
            GlobalSpoilingTimeMultiplier = data.GlobalSpoilingTimeMultiplier,
            GlobalCorpseDecompositionTimeMultiplier = data.GlobalCorpseDecompositionTimeMultiplier,
            GlobalItemDecompositionTimeMultiplier = data.GlobalItemDecompositionTimeMultiplier,
            CropDecaySpeedMultiplier = data.CropDecaySpeedMultiplier,
            CropGrowthSpeedMultiplier = data.CropGrowthSpeedMultiplier,
            LayEggIntervalMultiplier = data.LayEggIntervalMultiplier,
            PoopIntervalMultiplier = data.PoopIntervalMultiplier,
            HairGrowthSpeedMultiplier = data.HairGrowthSpeedMultiplier,
            CraftXPMultiplier = data.CraftXPMultiplier,
            GenericXPMultiplier = data.GenericXPMultiplier,
            HarvestXPMultiplier = data.HarvestXPMultiplier,
            KillXPMultiplier = data.KillXPMultiplier,
            SpecialXPMultiplier = data.SpecialXPMultiplier,
            DisableWeatherFog = data.DisableWeatherFog,
            StructureResistanceMultiplier = data.StructureResistanceMultiplier,
            StructureDamageRepairCooldown = data.StructureDamageRepairCooldown,
            PvPStructureDecay = data.PvPStructureDecay,
            PvPZoneStructureDamageMultiplier = data.PvPZoneStructureDamageMultiplier,
            MaxStructuresVisible = data.MaxStructuresVisible,
            PerPlatformMaxStructuresMultiplier = data.PerPlatformMaxStructuresMultiplier,
            MaxPlatformSaddleStructureLimit = data.MaxPlatformSaddleStructureLimit,
            OverrideStructurePlatformPrevention = data.OverrideStructurePlatformPrevention,
            FlyerPlatformAllowUnalignedDinoBasing = data.FlyerPlatformAllowUnalignedDinoBasing,
            PvEAllowStructuresAtSupplyDrops = data.PvEAllowStructuresAtSupplyDrops,
            EnableStructureDecayPvE = data.EnableStructureDecayPvE,
            PvEStructureDecayDestructionPeriod = data.PvEStructureDecayDestructionPeriod,
            PvEStructureDecayPeriodMultiplier = data.PvEStructureDecayPeriodMultiplier,
            AutoDestroyOldStructuresMultiplier = data.AutoDestroyOldStructuresMultiplier,
            ForceAllStructureLocking = data.ForceAllStructureLocking,
            PassiveDefensesDamageRiderlessDinos = data.PassiveDefensesDamageRiderlessDinos,
            OnlyAutoDestroyCoreStructures = data.OnlyAutoDestroyCoreStructures,
            OnlyDecayUnsnappedCoreStructures = data.OnlyDecayUnsnappedCoreStructures,
            FastDecayUnsnappedCoreStructures = data.FastDecayUnsnappedCoreStructures,
            DestroyUnconnectedWaterPipes = data.DestroyUnconnectedWaterPipes,
            DisableStructurePlacementCollision = data.DisableStructurePlacementCollision,
            FastDecayInterval = data.FastDecayInterval,
            LimitTurretsInRange = data.LimitTurretsInRange,
            LimitTurretsRange = data.LimitTurretsRange,
            LimitTurretsNum = data.LimitTurretsNum,
            HardLimitTurretsInRange = data.HardLimitTurretsInRange,
            OnlyAllowSpecifiedEngrams = data.OnlyAllowSpecifiedEngrams,
            AutoUnlockAllEngrams = data.AutoUnlockAllEngrams,
            PGM_Name = data.PGM_Name,
            SOTF_MaxNumberOfPlayersInTribe = data.SOTF_MaxNumberOfPlayersInTribe,
            SOTF_BattleNumOfTribesToStartGame = data.SOTF_BattleNumOfTribesToStartGame,
            SOTF_TimeToCollapseROD = data.SOTF_TimeToCollapseROD,
            SOTF_BattleAutoStartGameInterval = data.SOTF_BattleAutoStartGameInterval,
            SOTF_BattleAutoRestartGameInterval = data.SOTF_BattleAutoRestartGameInterval,
            SOTF_BattleSuddenDeathInterval = data.SOTF_BattleSuddenDeathInterval,

            DinoSpawnWeightMultipliers = new List<string>(data.DinoSpawnWeightMultipliers),
            TamedDinoClassDamageMultipliers = new List<string>(data.TamedDinoClassDamageMultipliers),
            TamedDinoClassResistanceMultipliers = new List<string>(data.TamedDinoClassResistanceMultipliers),
            DinoClassDamageMultipliers = new List<string>(data.DinoClassDamageMultipliers),
            DinoClassResistanceMultipliers = new List<string>(data.DinoClassResistanceMultipliers),
            NPCReplacements = new List<string>(data.NPCReplacements),
            PreventDinoTameClassNames = new List<string>(data.PreventDinoTameClassNames),
            HarvestResourceItemAmountClassMultipliers = new List<string>(data.HarvestResourceItemAmountClassMultipliers),
            OverrideNamedEngramEntries = new List<string>(data.OverrideNamedEngramEntries),
            ConfigOverrideItemCraftingCosts = new List<string>(data.ConfigOverrideItemCraftingCosts),
            ConfigAddNPCSpawnEntriesContainer = new List<string>(data.ConfigAddNPCSpawnEntriesContainer),
            ConfigSubtractNPCSpawnEntriesContainer = new List<string>(data.ConfigSubtractNPCSpawnEntriesContainer),
            ConfigOverrideNPCSpawnEntriesContainer = new List<string>(data.ConfigOverrideNPCSpawnEntriesContainer),
            ConfigOverrideSupplyCrateItems = new List<string>(data.ConfigOverrideSupplyCrateItems),
        };

        foreach (var modId in data.ModIds)
        {
            profile.ModIds.Add(modId);
        }

        return profile;
    }

    /// <summary>Creates an independent copy of this profile with a fresh <see cref="ProfileId"/>,
    /// so saving it via <see cref="ProfileStore"/> doesn't overwrite the original.</summary>
    public ServerProfile Duplicate(string? newProfileName = null)
    {
        var data = ToData();
        data.ProfileId = Guid.NewGuid();
        data.ProfileName = newProfileName ?? $"{ProfileName} (copy)";
        return FromData(data);
    }

    /// <summary>Builds a new profile by reading an existing GameUserSettings.ini/Game.ini pair
    /// from <paramref name="configDirectory"/> — for importing a server someone already has set
    /// up outside ArkKeeper. Missing files are treated as empty (their settings stay at
    /// ArkKeeper's defaults) rather than throwing.</summary>
    public static async Task<ServerProfile> ImportFromDirectoryAsync(string configDirectory, CancellationToken cancellationToken = default)
    {
        var gameUserSettingsText = await ReadIfExistsAsync(Path.Combine(configDirectory, "GameUserSettings.ini"), cancellationToken);
        var gameText = await ReadIfExistsAsync(Path.Combine(configDirectory, "Game.ini"), cancellationToken);

        var profile = new ServerProfile();
        profile.ImportFrom(IniDocument.Parse(gameUserSettingsText), IniDocument.Parse(gameText));
        return profile;
    }

    private static async Task<string> ReadIfExistsAsync(string path, CancellationToken cancellationToken) =>
        File.Exists(path) ? await File.ReadAllTextAsync(path, cancellationToken) : string.Empty;
}
