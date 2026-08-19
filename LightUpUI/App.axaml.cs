using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LightUpUI.Services;
using LightUpUI.Views;

namespace LightUpUI;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var searchService = new SearchService(
            [
                new ShortcutSearchProvider(),
                new PathExecutableSearchProvider()
            ]);
            var processLauncher = new WindowsProcessLauncher();
            var windowHost = new LauncherWindowHost();
            var viewModel = new ViewModels.MainViewModel(searchService, processLauncher, windowHost);
            var window = new MainWindow(viewModel);
            windowHost.AttachViewModel(viewModel);
            windowHost.Attach(window);

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = window;

            var hotkeyService = new WindowsGlobalHotkeyService();
            hotkeyService.HotkeyPressed += (_, _) =>
                Dispatcher.UIThread.Post(windowHost.Toggle);
            hotkeyService.Start();
            desktop.Exit += (_, _) => hotkeyService.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
