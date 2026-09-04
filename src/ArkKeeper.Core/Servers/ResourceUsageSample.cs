namespace ArkKeeper.Core.Servers;

/// <summary>One point-in-time reading of a running server process's resource usage.
/// <see cref="CpuPercent"/> is relative to the whole machine (all cores combined = 100%), matching
/// what Task Manager shows — not per-core.</summary>
public readonly record struct ResourceUsageSample(double CpuPercent, long WorkingSetBytes)
{
    public double WorkingSetGigabytes => WorkingSetBytes / 1024.0 / 1024.0 / 1024.0;
}
