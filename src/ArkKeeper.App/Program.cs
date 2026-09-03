using Avalonia;
using System;
using ArkKeeper.App.ViewModels;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Orchestration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArkKeeper.App;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using var host = BuildHost();
        App.Services = host.Services;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // Disposing ServerFleet here only releases its own RCON connections/handlers —
            // dedicated server processes it started keep running independently, same as the
            // original tool: closing the manager shouldn't kill servers players are on.
            host.StopAsync().GetAwaiter().GetResult();
        }
    }

    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(_ =>
        {
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ArkKeeper",
                "profiles");
            return new ProfileStore(dataDirectory);
        });
        builder.Services.AddSingleton<ServerFleet>();
        builder.Services.AddSingleton<MainViewModel>();

        return builder.Build();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
