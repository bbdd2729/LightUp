using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Services;

namespace LightUpUI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISearchLauncherSettingsStore _settingsStore;
    private readonly SearchLauncherSettings _settings;
    private readonly Action<SearchLauncherMode> _applySearchMode;
    private readonly Action<LauncherAppearanceSettings> _applyAppearance;
    private readonly Action<CategoryNavigationPlacement> _applyCategoryNavigationPlacement;
    private readonly Action<TileDensity> _applyTileDensity;
    private readonly Action<int> _applyMaxResults;
    private readonly Action<bool> _applySearchAllTileCategories;
    private readonly Func<bool, string?> _applyLaunchAtStartup;
    private readonly Func<string, string, string?> _applyHotkeys;
    private readonly Action<SearchLauncherSettings> _applyTileCornerSettings;

    public SettingsViewModel(
        ISearchLauncherSettingsStore settingsStore,
        SearchLauncherSettings settings,
        Action<SearchLauncherMode> applySearchMode,
        Action<LauncherAppearanceSettings>? applyAppearance = null,
        Action<CategoryNavigationPlacement>? applyCategoryNavigationPlacement = null,
        Action<TileDensity>? applyTileDensity = null,
        Action<int>? applyMaxResults = null,
        Action<bool>? applySearchAllTileCategories = null,
        Func<bool, string?>? applyLaunchAtStartup = null,
        Func<string, string, string?>? applyHotkeys = null,
        Action<SearchLauncherSettings>? applyTileCornerSettings = null)
    {
        _settingsStore = settingsStore;
        _settings = settings;
        _applySearchMode = applySearchMode;
        _applyAppearance = applyAppearance ?? (_ => { });
        _applyCategoryNavigationPlacement = applyCategoryNavigationPlacement ?? (_ => { });
        _applyTileDensity = applyTileDensity ?? (_ => { });
        _applyMaxResults = applyMaxResults ?? (_ => { });
        _applySearchAllTileCategories = applySearchAllTileCategories ?? (_ => { });
        _applyLaunchAtStartup = applyLaunchAtStartup ?? (_ => null);
        _applyHotkeys = applyHotkeys ?? ((_, _) => null);
        _applyTileCornerSettings = applyTileCornerSettings ?? (_ => { });
        _selectedSearchMode = settings.Mode;
        _selectedThemeMode = ThemePalettePolicy.NormalizeThemeMode(settings.Appearance.ThemeMode);
        _selectedColorPalette = ThemePalettePolicy.NormalizeColorPalette(settings.Appearance.ColorPalette);
        _customAccentColor = ThemePalettePolicy.NormalizeCustomAccentColor(settings.Appearance.CustomAccentColor);
        _selectedMaxResults = SearchResultLimitPolicy.Normalize(settings.MaxResults);
        _searchAllTileCategories = settings.SearchAllTileCategories;
        _saveQueryHistory = settings.SaveQueryHistory;
        _launchAtStartup = settings.LaunchAtStartup;
        _searchHotkey = settings.Hotkey;
        _tileLauncherHotkey = settings.TileLauncherHotkey;
        _enableTileCornerTrigger = settings.EnableTileCornerTrigger;
        _tileCornerTriggerDelayMilliseconds = TileCornerTriggerSettingsPolicy.NormalizeDelay(
            settings.TileCornerTriggerDelayMilliseconds);
        _closeTileLauncherOnPointerLeave = settings.CloseTileLauncherOnPointerLeave;
        _closeTileLauncherAfterLaunch = settings.CloseTileLauncherAfterLaunch;
        _selectedCategoryNavigationPlacement = CategoryNavigationPlacementPolicy.Normalize(
            settings.CategoryNavigationPlacement);
        _selectedTileDensity = TileDensityPolicy.Normalize(settings.Appearance.TileDensity);
    }

    public Array SearchModes { get; } = Enum.GetValues<SearchLauncherMode>();
    public Array ThemeModes { get; } = Enum.GetValues<LauncherThemeMode>();
    public Array ColorPalettes { get; } = Enum.GetValues<LauncherColorPalette>();
    public Array CategoryNavigationPlacements { get; } = Enum.GetValues<CategoryNavigationPlacement>();
    public Array TileDensities { get; } = Enum.GetValues<TileDensity>();
    public int[] ResultLimits { get; } = [10, 20, 30, 50, 100];
    public int[] TileCornerTriggerDelays { get; } = [300, 500, 700, 1000, 1500, 2000, 3000];

    [ObservableProperty]
    private SearchLauncherMode _selectedSearchMode;

    [ObservableProperty]
    private LauncherThemeMode _selectedThemeMode;

    [ObservableProperty]
    private LauncherColorPalette _selectedColorPalette;

    [ObservableProperty]
    private string _customAccentColor;

    public bool IsCustomAccentColorVisible => SelectedColorPalette == LauncherColorPalette.Custom;

    [ObservableProperty]
    private CategoryNavigationPlacement _selectedCategoryNavigationPlacement;

    [ObservableProperty]
    private TileDensity _selectedTileDensity;

    [ObservableProperty]
    private int _selectedMaxResults;

    [ObservableProperty]
    private bool _searchAllTileCategories;

    [ObservableProperty]
    private bool _saveQueryHistory;

    public int QueryHistoryCount => _settings.QueryHistory.Count;

    [ObservableProperty]
    private bool _launchAtStartup;

    [ObservableProperty]
    private string _searchHotkey;

    [ObservableProperty]
    private string _tileLauncherHotkey;

    [ObservableProperty]
    private bool _enableTileCornerTrigger;

    [ObservableProperty]
    private int _tileCornerTriggerDelayMilliseconds;

    [ObservableProperty]
    private bool _closeTileLauncherOnPointerLeave;

    [ObservableProperty]
    private bool _closeTileLauncherAfterLaunch;

    [ObservableProperty]
    private string _statusText = string.Empty;

    partial void OnSelectedColorPaletteChanged(LauncherColorPalette value)
        => OnPropertyChanged(nameof(IsCustomAccentColorVisible));

    [RelayCommand]
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var startupError = _applyLaunchAtStartup(LaunchAtStartup);
        if (!string.IsNullOrWhiteSpace(startupError))
        {
            StatusText = startupError;
            return;
        }
        if (!TryApplyHotkeys(out var searchHotkey, out var tileLauncherHotkey))
            return;

        _settings.Mode = SelectedSearchMode;
        _settings.Appearance.ThemeMode = ThemePalettePolicy.NormalizeThemeMode(SelectedThemeMode);
        _settings.Appearance.ColorPalette = ThemePalettePolicy.NormalizeColorPalette(SelectedColorPalette);
        _settings.Appearance.CustomAccentColor = ThemePalettePolicy.NormalizeCustomAccentColor(CustomAccentColor);
        _settings.CategoryNavigationPlacement = CategoryNavigationPlacementPolicy.Normalize(
            SelectedCategoryNavigationPlacement);
        _settings.Appearance.TileDensity = TileDensityPolicy.Normalize(SelectedTileDensity);
        _settings.MaxResults = SearchResultLimitPolicy.Normalize(SelectedMaxResults);
        _settings.SearchAllTileCategories = SearchAllTileCategories;
        _settings.SaveQueryHistory = SaveQueryHistory;
        if (!SaveQueryHistory)
            _settings.QueryHistory.Clear();
        OnPropertyChanged(nameof(QueryHistoryCount));
        _settings.LaunchAtStartup = LaunchAtStartup;
        _settings.Hotkey = searchHotkey;
        _settings.TileLauncherHotkey = tileLauncherHotkey;
        _settings.EnableTileCornerTrigger = EnableTileCornerTrigger;
        _settings.TileCornerTriggerDelayMilliseconds = TileCornerTriggerSettingsPolicy.NormalizeDelay(
            TileCornerTriggerDelayMilliseconds);
        _settings.CloseTileLauncherOnPointerLeave = CloseTileLauncherOnPointerLeave;
        _settings.CloseTileLauncherAfterLaunch = CloseTileLauncherAfterLaunch;
        await _settingsStore.SaveAsync(_settings, cancellationToken);
        _applySearchMode(SelectedSearchMode);
        _applyAppearance(_settings.Appearance);
        _applyCategoryNavigationPlacement(_settings.CategoryNavigationPlacement);
        _applyTileDensity(_settings.Appearance.TileDensity);
        _applyMaxResults(_settings.MaxResults);
        _applySearchAllTileCategories(_settings.SearchAllTileCategories);
        _applyTileCornerSettings(_settings);
        StatusText = "设置已保存";
    }

    [RelayCommand]
    public async Task ClearQueryHistoryAsync(CancellationToken cancellationToken = default)
    {
        _settings.QueryHistory.Clear();
        OnPropertyChanged(nameof(QueryHistoryCount));
        await _settingsStore.SaveAsync(_settings, cancellationToken);
        StatusText = "搜索历史已清空";
    }

    private bool TryApplyHotkeys(out string searchHotkey, out string tileLauncherHotkey)
    {
        searchHotkey = string.Empty;
        tileLauncherHotkey = string.Empty;

        if (!GlobalHotkeyParser.TryParse(SearchHotkey, out var searchGesture, out var searchError))
        {
            StatusText = searchError ?? "搜索栏快捷键无效。";
            return false;
        }

        if (!GlobalHotkeyParser.TryParse(TileLauncherHotkey, out var tileLauncherGesture, out var tileLauncherError))
        {
            StatusText = tileLauncherError ?? "磁贴启动器快捷键无效。";
            return false;
        }

        searchHotkey = searchGesture.ToConfigText();
        tileLauncherHotkey = tileLauncherGesture.ToConfigText();
        if (string.Equals(searchHotkey, tileLauncherHotkey, StringComparison.Ordinal))
        {
            StatusText = "搜索栏和磁贴启动器不能使用相同的全局快捷键。";
            return false;
        }

        var applyError = _applyHotkeys(searchHotkey, tileLauncherHotkey);
        if (!string.IsNullOrWhiteSpace(applyError))
        {
            StatusText = applyError;
            return false;
        }

        SearchHotkey = searchHotkey;
        TileLauncherHotkey = tileLauncherHotkey;
        return true;
    }
}
