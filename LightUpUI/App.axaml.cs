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
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = InitializeDesktopAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var tileStateStore = new JsonLauncherStateStore();
            var settingsStore = new SearchLauncherSettingsStore();
            var startupRegistration = new WindowsStartupRegistrationService();
            var searchSettings = await StartupSettingsLoader.LoadAsync(
                settingsStore,
                CancellationToken.None);
            LauncherThemeService.Apply(searchSettings.Appearance);
            var tileProvider = new TileSearchProvider(tileStateStore)
            {
                SearchAllTileCategories = searchSettings.SearchAllTileCategories
            };
            var fullProviders = new List<ISearchProvider>
            {
                tileProvider,
                new BuiltInActionSearchProvider(),
                new CalculatorSearchProvider(),
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
                categoryNavigationPlacement: searchSettings.CategoryNavigationPlacement,
                tileDensity: searchSettings.Appearance.TileDensity);
            var tileWindowHost = new TileLauncherWindowHost(tileViewModel);
            var tileWindow = new TileLauncherWindow(tileViewModel);
            tileWindowHost.Attach(tileWindow);
            EventHandler? activationHandler = null;
            if (Program.InstanceCoordinator is { } instanceCoordinator)
            {
                activationHandler = (_, _) => Dispatcher.UIThread.Post(tileWindowHost.Show);
                instanceCoordinator.ActivationRequested += activationHandler;
            }
            var hotkeyBindings = new LauncherHotkeyBindings(new WindowsGlobalHotkeyServiceFactory());
            ViewModels.MainViewModel? viewModel = null;
            MainWindow? window = null;
            var settingsViewModel = new ViewModels.SettingsViewModel(
                settingsStore,
                searchSettings,
                mode => viewModel!.SearchMode = mode,
                applyAppearance: appearance => LauncherThemeService.Apply(appearance),
                applyCategoryNavigationPlacement: placement => tileViewModel.CategoryNavigationPlacement = placement,
                applyTileDensity: density => tileViewModel.TileDensity = density,
                applyMaxResults: maxResults => viewModel!.MaxResults = maxResults,
                applySearchAllTileCategories: searchAllTileCategories => tileProvider.SearchAllTileCategories = searchAllTileCategories,
                applyLaunchAtStartup: startupRegistration.Apply,
                applyHotkeys: (searchHotkey, tileLauncherHotkey) =>
                    hotkeyBindings.TryApply(searchHotkey, tileLauncherHotkey, out var error) ? null : error);
            var actionHost = new LauncherActionHost(
                tileWindowHost,
                () =>
                {
                    ShowSettingsWindow(settingsViewModel, window);
                    return Task.CompletedTask;
                },
                copyText: text => window is null
                    ? Task.FromResult(LaunchResult.Failed("搜索窗口尚未准备完成"))
                    : window.CopyTextAsync(text));
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
            var searchWindowStateTracker = new LauncherWindowStateTracker(
                window,
                state => searchSettings.Appearance.SearchWindow = state,
                () => settingsStore.SaveAsync(searchSettings, CancellationToken.None),
                new Size(180, 180),
                new Size(1400, 900));
            var tileWindowStateTracker = new LauncherWindowStateTracker(
                tileWindow,
                state => searchSettings.Appearance.TileLauncherWindow = state,
                () => settingsStore.SaveAsync(searchSettings, CancellationToken.None),
                new Size(720, 420),
                new Size(1800, 1200));
            window.Opened += (_, _) => searchWindowStateTracker.Restore(searchSettings.Appearance.SearchWindow);
            tileWindow.Opened += (_, _) =>
            {
                tileWindowStateTracker.Restore(searchSettings.Appearance.TileLauncherWindow);
                _ = tileWindowHost.EnsureLoadedAsync();
            };
            hotkeyBindings.SearchHotkeyPressed += (_, _) =>
                Dispatcher.UIThread.Post(windowHost.Toggle);
            hotkeyBindings.TileLauncherHotkeyPressed += (_, _) =>
                Dispatcher.UIThread.Post(tileWindowHost.Toggle);
            if (!hotkeyBindings.TryApply(
                    searchSettings.Hotkey,
                    searchSettings.TileLauncherHotkey,
                    out _))
            {
                _ = hotkeyBindings.TryApply("alt+space", "alt+shift+space", out _);
            }

            CreateTrayIcon(
                desktop,
                windowHost,
                tileWindowHost,
                () =>
                {
                    ShowSettingsWindow(settingsViewModel, window);
                });

            Window startupWindow = LauncherStartupPolicy.MainSurface switch
            {
                LauncherStartupSurface.TileLauncher => tileWindow,
                LauncherStartupSurface.SearchLauncher => window,
                _ => tileWindow
            };
            desktop.MainWindow = startupWindow;
            if (LauncherStartupPolicy.ShouldShowMainSurfaceOnStartup)
                startupWindow.Show();

            desktop.Exit += (_, _) =>
            {
                searchWindowStateTracker.FlushAsync().GetAwaiter().GetResult();
                tileWindowStateTracker.FlushAsync().GetAwaiter().GetResult();
                hotkeyBindings.Dispose();
                if (activationHandler is not null && Program.InstanceCoordinator is { } instanceCoordinator)
                    instanceCoordinator.ActivationRequested -= activationHandler;
                searchWindowStateTracker.Dispose();
                tileWindowStateTracker.Dispose();
            };
        }
        catch
        {
            // Keep a visible recovery surface even if an unexpected bootstrap dependency fails.
            var recoveryWindow = new MainWindow();
            desktop.MainWindow = recoveryWindow;
            recoveryWindow.Show();
        }
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
        trayIcon.Menu = CreateTrayNativeMenu(
            () => searchHost.Show(),
            () => tileHost.Show(),
            openSettings,
            () => desktop.Shutdown());
        trayIcon.Clicked += (_, _) => quickMenu.Toggle();
        TrayIcon.SetIcons(this, new TrayIcons { trayIcon });
        desktop.Exit += (_, _) =>
        {
            quickMenu.Close();
            trayIcon.Dispose();
        };
    }

    private static NativeMenu CreateTrayNativeMenu(
        Action openSearch,
        Action openTiles,
        Action openSettings,
        Action exitApplication)
    {
        var menu = new NativeMenu();
        menu.Items.Add(CreateNativeMenuItem("打开搜索", openSearch));
        menu.Items.Add(CreateNativeMenuItem("打开磁贴启动器", openTiles));
        menu.Items.Add(CreateNativeMenuItem("设置", openSettings));
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(CreateNativeMenuItem("退出 LightUp", exitApplication));
        return menu;
    }

    private static NativeMenuItem CreateNativeMenuItem(string header, Action action)
    {
        var item = new NativeMenuItem(header);
        item.Click += (_, _) => action();
        return item;
    }
}
