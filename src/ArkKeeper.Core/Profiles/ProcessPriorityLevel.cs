namespace ArkKeeper.Core.Profiles;

/// <summary>OS scheduling priority to launch the server process at. Deliberately a small,
/// game-manager-friendly enum rather than exposing <c>System.Diagnostics.ProcessPriorityClass</c>
/// directly — skips the rarely-safe <c>RealTime</c> tier and keeps Core's public surface free of
/// a BCL type ArkKeeper.Core.Servers.ServerProcess maps this onto at launch time.</summary>
public enum ProcessPriorityLevel
{
    Idle,
    BelowNormal,
    Normal,
    AboveNormal,
    High,
}
