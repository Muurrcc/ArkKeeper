using System.Collections.ObjectModel;
using System.Linq;
using ArkKeeper.App.Services;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArkKeeper.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    /// <summary>1 minute of history at the app's 2s poll interval — matches
    /// <see cref="ServerRowViewModel"/>'s own per-server history length.</summary>
    private const int ResourceHistoryLength = 30;

    /// <summary>Pixel size the sparkline points are generated at — matches the drawing area given
    /// to the Polyline in DashboardView.axaml, so no runtime scaling/stretching is needed.</summary>
    private const double SparklineWidth = 240;
    private const double SparklineHeight = 48;

    private readonly ActivityLog _activityLog;
    private readonly Queue<double> _cpuHistory = new();
    private readonly Queue<double> _ramHistory = new();

    public DashboardViewModel(ObservableCollection<ServerRowViewModel> servers, ActivityLog activityLog)
    {
        Servers = servers;
        _activityLog = activityLog;
        Servers.CollectionChanged += (_, _) => RefreshSummary();
        Activity.CollectionChanged += (_, _) => HasNoActivity = Activity.Count == 0;
        HasNoActivity = Activity.Count == 0;
        RefreshSummary();
    }

    public ObservableCollection<ServerRowViewModel> Servers { get; }

    public ObservableCollection<ActivityEntry> Activity => _activityLog.Entries;

    public int ProcessorCount { get; } = Environment.ProcessorCount;

    /// <summary>A reasonable proxy for total physical RAM available to this machine — .NET has no
    /// direct "total system memory" API, but the GC's own memory-pressure figure tracks it
    /// closely and is what the runtime itself budgets against.</summary>
    public double TotalSystemRamGigabytes { get; } =
        GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0 / 1024.0;

    [ObservableProperty]
    public partial bool HasNoActivity { get; set; }

    [ObservableProperty]
    public partial bool HasNoServers { get; set; }

    [ObservableProperty]
    public partial int ServerCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyRunning))]
    public partial int RunningCount { get; set; }

    [ObservableProperty]
    public partial int TotalCapacity { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CpuPercentDisplay))]
    public partial double TotalCpuPercent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RamGigabytesDisplay))]
    public partial double TotalRamGigabytes { get; set; }

    /// <summary>Pre-formatted display strings, bound directly to a plain TextBlock.Text rather
    /// than composed from multiple &lt;Run&gt; bindings in XAML — Avalonia's Run/Inline bindings
    /// didn't reliably repaint on every value change here (confirmed: the sparkline, bound as a
    /// normal control property, updated live every tick; the Run-composed percentage text next to
    /// it visibly did not), so the numeric readout looked frozen while the trend line kept moving.
    /// Formatting in the ViewModel and binding one TextBlock.Text sidesteps that entirely.</summary>
    public string CpuPercentDisplay => TotalCpuPercent.ToString("F0");

    public string RamGigabytesDisplay => TotalRamGigabytes.ToString("F1");

    public string TotalSystemRamGigabytesDisplay => TotalSystemRamGigabytes.ToString("F0");

    [ObservableProperty]
    public partial IReadOnlyList<Point> CpuSparklinePoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> RamSparklinePoints { get; set; } = [];

    /// <summary>Drives the "Live" pulse animation on the RUNNING NOW icon — only worth animating
    /// when there's actually something running to draw attention to.</summary>
    public bool IsAnyRunning => RunningCount > 0;

    /// <summary>Re-derives the running count and aggregate resource usage from each row's live
    /// state. Called alongside <see cref="ServersViewModel.RefreshAll"/> on the same poll timer,
    /// after it — so each row's CPU/RAM sample for this tick is already up to date.</summary>
    public void RefreshSummary()
    {
        HasNoServers = Servers.Count == 0;
        ServerCount = Servers.Count;
        RunningCount = Servers.Count(s => s.IsRunning);
        TotalCapacity = Servers.Sum(s => s.Profile.MaxPlayers);

        TotalCpuPercent = Servers.Sum(s => s.CpuPercent);
        TotalRamGigabytes = Servers.Sum(s => s.RamGigabytes);

        _cpuHistory.Enqueue(TotalCpuPercent);
        _ramHistory.Enqueue(TotalRamGigabytes);
        while (_cpuHistory.Count > ResourceHistoryLength)
        {
            _cpuHistory.Dequeue();
        }
        while (_ramHistory.Count > ResourceHistoryLength)
        {
            _ramHistory.Dequeue();
        }

        var cpuValues = _cpuHistory.ToList();
        var ramValues = _ramHistory.ToList();
        CpuSparklinePoints = SparklineMath.ToPoints(cpuValues, SparklineWidth, SparklineHeight, Math.Max(10, cpuValues.Count == 0 ? 0 : cpuValues.Max()));
        RamSparklinePoints = SparklineMath.ToPoints(ramValues, SparklineWidth, SparklineHeight, Math.Max(1, ramValues.Count == 0 ? 0 : ramValues.Max()));
    }
}
