using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ArkKeeper.App.ViewModels;
using ArkKeeper.App.Views;

namespace ArkKeeper.App;

/// <summary>
/// Maps each ViewModel to its View explicitly. The original template's implementation used
/// reflection (Type.GetType by string name), which silently breaks under trimming/NativeAOT —
/// the trimmer removes View types nothing statically references. This has none, so it stays
/// correct under a trimmed publish (see Phase 5 notes: `dotnet publish -p:PublishTrimmed=true`).
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> Factories = new()
    {
        [typeof(DashboardViewModel)] = () => new DashboardView(),
        [typeof(ServersViewModel)] = () => new ServersView(),
        [typeof(SettingsViewModel)] = () => new SettingsView(),
        [typeof(ProfileEditorViewModel)] = () => new ProfileEditorView(),
        [typeof(RconConsoleViewModel)] = () => new RconConsoleView(),
        [typeof(PlayersViewModel)] = () => new PlayersView(),
        [typeof(BackupsViewModel)] = () => new BackupsView(),
        [typeof(SchedulerViewModel)] = () => new SchedulerView(),
        [typeof(ModsViewModel)] = () => new ModsView(),
    };

    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        return Factories.TryGetValue(param.GetType(), out var factory)
            ? factory()
            : new TextBlock { Text = "Not Found: " + param.GetType().Name };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
