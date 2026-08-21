using System;
using Avalonia;

using LightUpUI.Services;

namespace LightUpUI;

internal sealed class Program
{
    internal static SingleInstanceCoordinator? InstanceCoordinator { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using var instanceCoordinator = new SingleInstanceCoordinator("LightUpUI");
        if (!instanceCoordinator.IsPrimaryInstance)
        {
            instanceCoordinator.TrySignalPrimary(TimeSpan.FromSeconds(2));
            return;
        }

        InstanceCoordinator = instanceCoordinator;
        instanceCoordinator.StartListening();
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            InstanceCoordinator = null;
        }
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
