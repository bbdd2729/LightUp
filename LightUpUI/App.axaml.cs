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

            var tileViewModel = new ViewModels.TileLauncherViewModel(tileStateStore, launchProcess);
            var tileWindowHost = new TileLauncherWindowHost(tileViewModel);
            var tileWindow = new TileLauncherWindow(tileViewModel);
            tileWindowHost.Attach(tileWindow);

            var settingsStore = new SearchLauncherSettingsStore();
            ViewModels.MainViewModel? viewModel = null;
            MainWindow? window = null;
            var settingsViewModel = new ViewModels.SettingsViewModel(
                settingsStore,
                searchSettings,
                mode => viewModel!.SearchMode = mode);
            var actionHost = new LauncherActionHost(
                tileWindowHost,
                () =>
                {
                    var settingsWindow = new SettingsWindow(settingsViewModel);
                    settingsWindow.Show(window!);
                    settingsWindow.Activate();
                    return Task.CompletedTask;
                });
            IProcessLauncher processLauncher = new LauncherProcessRouter(
                launchProcess,
                actionHost.ExecuteAsync);
            viewModel = new ViewModels.MainViewModel(searchService, processLauncher, windowHost)
            {
                SearchMode = searchSettings.Mode
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
                    var settingsWindow = new SettingsWindow(settingsViewModel);
                    settingsWindow.Show(window);
                    settingsWindow.Activate();
                });

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
        var openSearchItem = new NativeMenuItem("打开搜索") { Gesture = KeyGesture.Parse("Alt+Space") };
        var openTilesItem = new NativeMenuItem("打开磁贴启动器") { Gesture = KeyGesture.Parse("Alt+Shift+Space") };
        var settingsItem = new NativeMenuItem("打开设置");
        var exitItem = new NativeMenuItem("退出");
        var trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(bitmap),
            ToolTipText = "LightUp 启动器",
            Menu = new NativeMenu
            {
                    Items =
                    {
                    openSearchItem,
                    openTilesItem,
                    new NativeMenuItemSeparator(),
                    settingsItem,
                    new NativeMenuItemSeparator(),
                    exitItem
                }
            }
        };
        openSearchItem.Click += (_, _) => searchHost.Show();
        openTilesItem.Click += (_, _) => tileHost.Show();
        settingsItem.Click += (_, _) => openSettings();
        exitItem.Click += (_, _) => desktop.Shutdown();
        TrayIcon.SetIcons(this, new TrayIcons { trayIcon });
        desktop.Exit += (_, _) => trayIcon.Dispose();
    }
}
