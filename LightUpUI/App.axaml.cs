using Avalonia;
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using LightUpUI.Models;
using LightUpUI.Plugins;
using LightUpUI.Services;
using LightUpUI.Views;
using LightUpUI.ViewModels;

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
            var searchSettings = new SearchLauncherSettingsStore()
                .LoadAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            var tileProvider = new TileSearchProvider(tileStateStore)
            {
                SearchAllTileCategories = searchSettings.SearchAllTileCategories
            };
            var fullProviders = new List<ISearchProvider>
            {
                tileProvider,
                new BuiltInActionSearchProvider(),
                new ShortcutSearchProvider(),
                new PathExecutableSearchProvider()
            };
            var pluginHost = SearchPluginHost.LoadFromDirectory(
                Path.Combine(AppContext.BaseDirectory, "Plugins"),
                searchSettings);
            fullProviders.AddRange(pluginHost.Providers);
            var searchService = new SearchService(fullProviders, [tileProvider]);
            IProcessLauncher launchProcess = new UsageTrackingProcessLauncher(
                new WindowsProcessLauncher(),
                new TileUsageService(tileStateStore));
            var windowHost = new LauncherWindowHost();

            var tileViewModel = new ViewModels.TileLauncherViewModel(
                tileStateStore,
                launchProcess,
                categoryNavigationPlacement: searchSettings.CategoryNavigationPlacement);
            var tileWindowHost = new TileLauncherWindowHost(tileViewModel);
            var tileWindow = new TileLauncherWindow(tileViewModel);
            tileWindowHost.Attach(tileWindow);
            tileWindow.Opened += (_, _) => _ = tileWindowHost.EnsureLoadedAsync();

            var settingsStore = new SearchLauncherSettingsStore();
            ViewModels.MainViewModel? viewModel = null;
            MainWindow? window = null;
            var settingsViewModel = new ViewModels.SettingsViewModel(
                settingsStore,
                searchSettings,
                mode => viewModel!.SearchMode = mode,
                placement => tileViewModel.CategoryNavigationPlacement = placement,
                maxResults => viewModel!.MaxResults = maxResults,
                searchAllTileCategories => tileProvider.SearchAllTileCategories = searchAllTileCategories);
            var actionHost = new LauncherActionHost(
                tileWindowHost,
                () =>
                {
                    ShowSettingsWindow(settingsViewModel, window);
                    return Task.CompletedTask;
                });
            IProcessLauncher processLauncher = new LauncherProcessRouter(
                launchProcess,
                actionHost.ExecuteAsync);
            viewModel = new ViewModels.MainViewModel(searchService, processLauncher, windowHost)
            {
                SearchMode = searchSettings.Mode,
                MaxResults = searchSettings.MaxResults
            };
            window = new MainWindow(viewModel);
            windowHost.AttachViewModel(viewModel);
            windowHost.Attach(window);

            CreateTrayIcon(
                desktop,
                windowHost,
                tileWindowHost,
                () =>
                {
                    ShowSettingsWindow(settingsViewModel, window);
                });

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = LauncherStartupPolicy.MainSurface switch
            {
                LauncherStartupSurface.TileLauncher => tileWindow,
                LauncherStartupSurface.SearchLauncher => window,
                _ => tileWindow
            };

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

    private static void ShowSettingsWindow(
        ViewModels.SettingsViewModel viewModel,
        Window? owner)
    {
        var settingsWindow = new SettingsWindow(viewModel);
        if (owner is not null && WindowOwnerPolicy.CanUseOwner(owner.IsVisible))
            settingsWindow.Show(owner);
        else
            settingsWindow.Show();

        settingsWindow.Activate();
    }

    private void CreateTrayIcon(
        IClassicDesktopStyleApplicationLifetime desktop,
        ILauncherWindowHost searchHost,
        ITileLauncherWindowHost tileHost,
        Action openSettings)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var iconUri = new Uri("avares://LightUpUI/Assets/avalonia-logo.ico");
        using var stream = AssetLoader.Open(iconUri);
        var bitmap = new Bitmap(stream);
        TrayMenuWindow? quickMenu = null;
        var menuViewModel = new TrayMenuViewModel(
            () =>
            {
                quickMenu?.Hide();
                searchHost.Show();
            },
            () =>
            {
                quickMenu?.Hide();
                tileHost.Show();
            },
            () =>
            {
                quickMenu?.Hide();
                openSettings();
            },
            () => desktop.Shutdown());
        quickMenu = new TrayMenuWindow(menuViewModel);
        var trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(bitmap),
            ToolTipText = "LightUp 启动器 · 点击打开快捷菜单"
        };
        trayIcon.Clicked += (_, _) => quickMenu.Toggle();
        TrayIcon.SetIcons(this, new TrayIcons { trayIcon });
        desktop.Exit += (_, _) =>
        {
            quickMenu.Close();
            trayIcon.Dispose();
        };
    }
}
