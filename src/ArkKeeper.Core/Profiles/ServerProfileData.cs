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
    public bool DisableBattlEye { get; set; }
    public ProcessPriorityLevel ProcessPriority { get; set; } = ProcessPriorityLevel.Normal;
    public int CpuCoreLimit { get; set; }
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

    // Extended settings (ported from the original tool) — see ServerProfile.cs for the matching
    // [ObservableProperty]/[IniSetting] declarations; these must stay in sync field-for-field.
    public string SpectatorPassword { get; set; } = "";
    public string ServerIP { get; set; } = "";
    public string BanListURL { get; set; } = "http://arkdedicated.com/banlist.txt";
    public int KickIdlePlayersPeriod { get; set; } = 3600;
    public int RCONServerGameLogBuffer { get; set; } = 600;
    public bool AdminLogging { get; set; } = false;
    public string ServerModIds { get; set; } = "";
    public int ExtinctionEventTimeInterval { get; set; } = 2592000;
    public int ExtinctionEventUTC { get; set; } = 0;
    public float AutoSavePeriodMinutes { get; set; } = 15.0f;
    public string MOTD { get; set; } = "";
    public int MOTDDuration { get; set; } = 20;
    public int ServerAutoForceRespawnWildDinosInterval { get; set; } = 86400;
    public int MaxTribeLogs { get; set; } = 100;
    public bool TribeLogDestroyedEnemyStructures { get; set; } = false;
    public bool AllowHideDamageSourceFromLogs { get; set; } = false;
    public bool AllowCaveBuildingPvE { get; set; } = false;
    public bool DisableFriendlyFirePvP { get; set; } = false;
    public bool DisableFriendlyFirePvE { get; set; } = false;
    public bool DisableLootCrates { get; set; } = false;
    public bool EnableExtraStructurePreventionVolumes { get; set; } = false;
    public float OverrideOfficialDifficulty { get; set; } = 4.0f;
    public int MaxNumberOfPlayersInTribe { get; set; } = 70;
    public bool EnableTributeDownloads { get; set; } = false;
    public bool PreventDownloadSurvivors { get; set; } = true;
    public bool PreventDownloadItems { get; set; } = true;
    public bool PreventDownloadDinos { get; set; } = true;
    public bool PreventUploadSurvivors { get; set; } = true;
    public bool PreventUploadItems { get; set; } = true;
    public bool PreventUploadDinos { get; set; } = true;
    public int TributeCharacterExpirationSeconds { get; set; } = 86400;
    public int TributeItemExpirationSeconds { get; set; } = 86400;
    public int TributeDinoExpirationSeconds { get; set; } = 86400;
    public int MinimumDinoReuploadInterval { get; set; } = 43200;
    public bool CrossARKAllowForeignDinoDownloads { get; set; } = false;
    public bool IncreasePvPRespawnInterval { get; set; } = false;
    public int IncreasePvPRespawnIntervalCheckPeriod { get; set; } = 300;
    public float IncreasePvPRespawnIntervalMultiplier { get; set; } = 1.0f;
    public int IncreasePvPRespawnIntervalBaseAmount { get; set; } = 60;
    public bool PreventOfflinePvP { get; set; } = false;
    public int PreventOfflinePvPInterval { get; set; } = 900;
    public int PreventOfflinePvPConnectionInvincibleInterval { get; set; } = 5;
    public bool AutoPvETimer { get; set; } = false;
    public bool AutoPvEUseSystemTime { get; set; } = false;
    public int AutoPvEStartTimeSeconds { get; set; } = 0;
    public int AutoPvEStopTimeSeconds { get; set; } = 0;
    public bool AllowTribeWarPvE { get; set; } = true;
    public bool AllowTribeWarCancelPvE { get; set; } = false;
    public bool AllowTribeAlliances { get; set; } = true;
    public int MaxAlliancesPerTribe { get; set; } = 10;
    public int MaxTribesPerAlliance { get; set; } = 10;
    public bool AllowCustomRecipes { get; set; } = true;
    public float CustomRecipeEffectivenessMultiplier { get; set; } = 1.0f;
    public float CustomRecipeSkillMultiplier { get; set; } = 1.0f;
    public bool EnableDiseases { get; set; } = true;
    public bool NonPermanentDiseases { get; set; } = false;
    public int NPCNetworkStasisRangeScalePlayerCountStart { get; set; } = 70;
    public int NPCNetworkStasisRangeScalePlayerCountEnd { get; set; } = 120;
    public float NPCNetworkStasisRangeScalePercentEnd { get; set; } = 0.5f;
    public bool UseCorpseLocator { get; set; } = false;
    public bool PreventSpawnAnimations { get; set; } = false;
    public bool AllowUnlimitedRespecs { get; set; } = false;
    public bool AllowPlatformSaddleMultiFloors { get; set; } = false;
    public float OxygenSwimSpeedStatMultiplier { get; set; } = 1.0f;
    public float SupplyCrateLootQualityMultiplier { get; set; } = 1.0f;
    public float FishingLootQualityMultiplier { get; set; } = 1.0f;
    public float UseCorpseLifeSpanMultiplier { get; set; } = 1.0f;
    public float GlobalPoweredBatteryDurabilityDecreasePerSecond { get; set; } = 4.0f;
    public int TribeNameChangeCooldown { get; set; } = 15;
    public bool RandomSupplyCratePoints { get; set; } = false;
    public bool EnableGlobalVoiceChat { get; set; } = false;
    public bool EnableProximityChat { get; set; } = false;
    public bool EnablePlayerLeaveNotifications { get; set; } = false;
    public bool EnablePlayerJoinedNotifications { get; set; } = false;
    public bool AllowHUD { get; set; } = true;
    public bool AllowPVPGamma { get; set; } = false;
    public bool AllowPvEGamma { get; set; } = false;
    public bool ShowFloatingDamageText { get; set; } = false;
    public bool AllowHitMarkers { get; set; } = true;
    public bool EnableFlyerCarry { get; set; } = false;
    public int OverrideMaxExperiencePointsPlayer { get; set; } = 0;
    public float PlayerResistanceMultiplier { get; set; } = 1.0f;
    public float PlayerCharacterWaterDrainMultiplier { get; set; } = 1.0f;
    public float PlayerCharacterFoodDrainMultiplier { get; set; } = 1.0f;
    public float PlayerCharacterStaminaDrainMultiplier { get; set; } = 1.0f;
    public float PlayerCharacterHealthRecoveryMultiplier { get; set; } = 1.0f;
    public float PlayerHarvestingDamageMultiplier { get; set; } = 1.0f;
    public float CraftingSkillBonusMultiplier { get; set; } = 1.0f;
    public int OverrideMaxExperiencePointsDino { get; set; } = 0;
    public float TamedDinoDamageMultiplier { get; set; } = 1.0f;
    public float DinoResistanceMultiplier { get; set; } = 1.0f;
    public float TamedDinoResistanceMultiplier { get; set; } = 1.0f;
    public int MaxTamedDinos { get; set; } = 4000;
    public float DinoCharacterFoodDrainMultiplier { get; set; } = 1.0f;
    public float DinoCharacterStaminaDrainMultiplier { get; set; } = 1.0f;
    public float DinoCharacterHealthRecoveryMultiplier { get; set; } = 1.0f;
    public float DinoCountMultiplier { get; set; } = 1.0f;
    public float DinoHarvestingDamageMultiplier { get; set; } = 3.0f;
    public float DinoTurretDamageMultiplier { get; set; } = 1.0f;
    public bool AllowRaidDinoFeeding { get; set; } = false;
    public float RaidDinoCharacterFoodDrainMultiplier { get; set; } = 1.0f;
    public bool AllowFlyingStaminaRecovery { get; set; } = false;
    public bool PreventMateBoost { get; set; } = false;
    public bool DisableDinoDecayPvE { get; set; } = false;
    public bool DisableDinoDecayPvP { get; set; } = true;
    public bool AutoDestroyDecayedDinos { get; set; } = false;
    public float PvEDinoDecayPeriodMultiplier { get; set; } = 1.0f;
    public bool AllowMultipleAttachedC4 { get; set; } = false;
    public bool DisableDinoRiding { get; set; } = false;
    public bool DisableDinoTaming { get; set; } = false;
    public float MaxPersonalTamedDinos { get; set; } = 40.0f;
    public int PersonalTamedDinosSaddleStructureCost { get; set; } = 19;
    public bool UseTameLimitForStructuresOnly { get; set; } = false;
    public float MatingIntervalMultiplier { get; set; } = 1.0f;
    public float EggHatchSpeedMultiplier { get; set; } = 1.0f;
    public float BabyMatureSpeedMultiplier { get; set; } = 1.0f;
    public float BabyFoodConsumptionSpeedMultiplier { get; set; } = 1.0f;
    public bool DisableImprintDinoBuff { get; set; } = false;
    public bool AllowAnyoneBabyImprintCuddle { get; set; } = false;
    public float BabyImprintingStatScaleMultiplier { get; set; } = 1.0f;
    public float BabyCuddleIntervalMultiplier { get; set; } = 1.0f;
    public float BabyCuddleGracePeriodMultiplier { get; set; } = 1.0f;
    public float BabyCuddleLoseImprintQualitySpeedMultiplier { get; set; } = 1.0f;
    public float WildDinoCharacterFoodDrainMultiplier { get; set; } = 1.0f;
    public float TamedDinoCharacterFoodDrainMultiplier { get; set; } = 1.0f;
    public float WildDinoTorporDrainMultiplier { get; set; } = 1.0f;
    public float TamedDinoTorporDrainMultiplier { get; set; } = 1.0f;
    public float PassiveTameIntervalMultiplier { get; set; } = 1.0f;
    public float ResourceNoReplenishRadiusPlayers { get; set; } = 1.0f;
    public float ResourceNoReplenishRadiusStructures { get; set; } = 1.0f;
    public float HarvestHealthMultiplier { get; set; } = 1.0f;
    public bool UseOptimizedHarvestingHealth { get; set; } = false;
    public bool ClampResourceHarvestDamage { get; set; } = false;
    public bool ClampItemSpoilingTimes { get; set; } = false;
    public float BaseTemperatureMultiplier { get; set; } = 1.0f;
    public float DayTimeSpeedScale { get; set; } = 1.0f;
    public float NightTimeSpeedScale { get; set; } = 1.0f;
    public float GlobalSpoilingTimeMultiplier { get; set; } = 1.0f;
    public float GlobalCorpseDecompositionTimeMultiplier { get; set; } = 1.0f;
    public float GlobalItemDecompositionTimeMultiplier { get; set; } = 1.0f;
    public float CropDecaySpeedMultiplier { get; set; } = 1.0f;
    public float CropGrowthSpeedMultiplier { get; set; } = 1.0f;
    public float LayEggIntervalMultiplier { get; set; } = 1.0f;
    public float PoopIntervalMultiplier { get; set; } = 1.0f;
    public float HairGrowthSpeedMultiplier { get; set; } = 1.0f;
    public float CraftXPMultiplier { get; set; } = 1.0f;
    public float GenericXPMultiplier { get; set; } = 1.0f;
    public float HarvestXPMultiplier { get; set; } = 1.0f;
    public float KillXPMultiplier { get; set; } = 1.0f;
    public float SpecialXPMultiplier { get; set; } = 1.0f;
    public bool DisableWeatherFog { get; set; } = false;
    public float StructureResistanceMultiplier { get; set; } = 1.0f;
    public int StructureDamageRepairCooldown { get; set; } = 180;
    public bool PvPStructureDecay { get; set; } = false;
    public float PvPZoneStructureDamageMultiplier { get; set; } = 6.0f;
    public float MaxStructuresVisible { get; set; } = 10500f;
    public float PerPlatformMaxStructuresMultiplier { get; set; } = 1.0f;
    public int MaxPlatformSaddleStructureLimit { get; set; } = 50;
    public bool OverrideStructurePlatformPrevention { get; set; } = false;
    public bool FlyerPlatformAllowUnalignedDinoBasing { get; set; } = false;
    public bool PvEAllowStructuresAtSupplyDrops { get; set; } = false;
    public bool EnableStructureDecayPvE { get; set; } = false;
    public float PvEStructureDecayDestructionPeriod { get; set; } = 0f;
    public float PvEStructureDecayPeriodMultiplier { get; set; } = 1.0f;
    public float AutoDestroyOldStructuresMultiplier { get; set; } = 0.0f;
    public bool ForceAllStructureLocking { get; set; } = false;
    public bool PassiveDefensesDamageRiderlessDinos { get; set; } = false;
    public bool OnlyAutoDestroyCoreStructures { get; set; } = false;
    public bool OnlyDecayUnsnappedCoreStructures { get; set; } = false;
    public bool FastDecayUnsnappedCoreStructures { get; set; } = false;
    public bool DestroyUnconnectedWaterPipes { get; set; } = false;
    public bool DisableStructurePlacementCollision { get; set; } = false;
    public int FastDecayInterval { get; set; } = 43200;
    public bool LimitTurretsInRange { get; set; } = false;
    public int LimitTurretsRange { get; set; } = 10000;
    public int LimitTurretsNum { get; set; } = 100;
    public bool HardLimitTurretsInRange { get; set; } = false;
    public bool OnlyAllowSpecifiedEngrams { get; set; } = false;
    public bool AutoUnlockAllEngrams { get; set; } = false;
    public string PGM_Name { get; set; } = "";
    public int SOTF_MaxNumberOfPlayersInTribe { get; set; } = 2;
    public int SOTF_BattleNumOfTribesToStartGame { get; set; } = 15;
    public int SOTF_TimeToCollapseROD { get; set; } = 9000;
    public int SOTF_BattleAutoStartGameInterval { get; set; } = 60;
    public int SOTF_BattleAutoRestartGameInterval { get; set; } = 45;
    public int SOTF_BattleSuddenDeathInterval { get; set; } = 300;

    // Override lists — see ServerProfile.cs's matching region for why these are List<string>.
    public List<string> DinoSpawnWeightMultipliers { get; set; } = new();
    public List<string> TamedDinoClassDamageMultipliers { get; set; } = new();
    public List<string> TamedDinoClassResistanceMultipliers { get; set; } = new();
    public List<string> DinoClassDamageMultipliers { get; set; } = new();
    public List<string> DinoClassResistanceMultipliers { get; set; } = new();
    public List<string> NPCReplacements { get; set; } = new();
    public List<string> PreventDinoTameClassNames { get; set; } = new();
    public List<string> HarvestResourceItemAmountClassMultipliers { get; set; } = new();
    public List<string> OverrideNamedEngramEntries { get; set; } = new();
    public List<string> ConfigOverrideItemCraftingCosts { get; set; } = new();
    public List<string> ConfigAddNPCSpawnEntriesContainer { get; set; } = new();
    public List<string> ConfigSubtractNPCSpawnEntriesContainer { get; set; } = new();
    public List<string> ConfigOverrideNPCSpawnEntriesContainer { get; set; } = new();
    public List<string> ConfigOverrideSupplyCrateItems { get; set; } = new();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerProfileData))]
internal sealed partial class ServerProfileDataJsonContext : JsonSerializerContext
{
}
