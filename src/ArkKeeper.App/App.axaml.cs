using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ArkKeeper.App.ViewModels;
using ArkKeeper.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ArkKeeper.App;

public partial class App : Application
{
    /// <summary>Set from Program.Main before Avalonia starts. Static because Avalonia itself
    /// constructs <see cref="App"/> — there's no constructor injection hook for it.</summary>
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}