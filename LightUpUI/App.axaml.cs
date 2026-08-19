using Avalonia;
using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Collections.Generic;
using System.IO;
using LightUpUI.Models;
using LightUpUI.Plugins;
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
            var tileStateStore = new JsonLauncherStateStore();
            var tileProvider = new TileSearchProvider(tileStateStore);
            var searchSettings = new SearchLauncherSettingsStore()
                .LoadAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            var fullProviders = new List<ISearchProvider>
            {
                new ShortcutSearchProvider(),
                new PathExecutableSearchProvider()
            };
            var pluginHost = SearchPluginHost.LoadFromDirectory(
                Path.Combine(AppContext.BaseDirectory, "Plugins"),
                searchSettings);
            fullProviders.AddRange(pluginHost.Providers);
            var searchService = new SearchService(fullProviders, [tileProvider]);
            var processLauncher = new WindowsProcessLauncher();
            var windowHost = new LauncherWindowHost();
            var viewModel = new ViewModels.MainViewModel(searchService, processLauncher, windowHost);
            viewModel.SearchMode = searchSettings.Mode;
            var window = new MainWindow(viewModel);
            windowHost.AttachViewModel(viewModel);
            windowHost.Attach(window);

            var tileViewModel = new ViewModels.TileLauncherViewModel(tileStateStore, processLauncher);
            var tileWindowHost = new TileLauncherWindowHost(tileViewModel);
            var tileWindow = new TileLauncherWindow(tileViewModel);
            tileWindowHost.Attach(tileWindow);

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = window;

            var hotkeyService = new WindowsGlobalHotkeyService();
            hotkeyService.HotkeyPressed += (_, _) =>
                Dispatcher.UIThread.Post(windowHost.Toggle);
            hotkeyService.Start();

            var tileHotkeyService = new WindowsGlobalHotkeyService(includeShift: true);
            tileHotkeyService.HotkeyPressed += (_, _) =>
                Dispatcher.UIThread.Post(tileWindowHost.Toggle);
            tileHotkeyService.Start();

            desktop.Exit += (_, _) =>
            {
                hotkeyService.Dispose();
                tileHotkeyService.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
