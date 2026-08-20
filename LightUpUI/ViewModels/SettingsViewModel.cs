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
    private readonly Action<CategoryNavigationPlacement> _applyCategoryNavigationPlacement;
    private readonly Action<int> _applyMaxResults;
    private readonly Action<bool> _applySearchAllTileCategories;
    private readonly Func<string, string, string?> _applyHotkeys;

    public SettingsViewModel(
        ISearchLauncherSettingsStore settingsStore,
        SearchLauncherSettings settings,
        Action<SearchLauncherMode> applySearchMode,
        Action<CategoryNavigationPlacement>? applyCategoryNavigationPlacement = null,
        Action<int>? applyMaxResults = null,
        Action<bool>? applySearchAllTileCategories = null,
        Func<string, string, string?>? applyHotkeys = null)
    {
        _settingsStore = settingsStore;
        _settings = settings;
        _applySearchMode = applySearchMode;
        _applyCategoryNavigationPlacement = applyCategoryNavigationPlacement ?? (_ => { });
        _applyMaxResults = applyMaxResults ?? (_ => { });
        _applySearchAllTileCategories = applySearchAllTileCategories ?? (_ => { });
        _applyHotkeys = applyHotkeys ?? ((_, _) => null);
        _selectedSearchMode = settings.Mode;
        _selectedMaxResults = SearchResultLimitPolicy.Normalize(settings.MaxResults);
        _searchAllTileCategories = settings.SearchAllTileCategories;
        _searchHotkey = settings.Hotkey;
        _tileLauncherHotkey = settings.TileLauncherHotkey;
        _selectedCategoryNavigationPlacement = CategoryNavigationPlacementPolicy.Normalize(
            settings.CategoryNavigationPlacement);
    }

    public Array SearchModes { get; } = Enum.GetValues<SearchLauncherMode>();
    public Array CategoryNavigationPlacements { get; } = Enum.GetValues<CategoryNavigationPlacement>();
    public int[] ResultLimits { get; } = [10, 20, 30, 50, 100];

    [ObservableProperty]
    private SearchLauncherMode _selectedSearchMode;

    [ObservableProperty]
    private CategoryNavigationPlacement _selectedCategoryNavigationPlacement;

    [ObservableProperty]
    private int _selectedMaxResults;

    [ObservableProperty]
    private bool _searchAllTileCategories;

    [ObservableProperty]
    private string _searchHotkey;

    [ObservableProperty]
    private string _tileLauncherHotkey;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [RelayCommand]
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (!TryApplyHotkeys(out var searchHotkey, out var tileLauncherHotkey))
            return;

        _settings.Mode = SelectedSearchMode;
        _settings.CategoryNavigationPlacement = CategoryNavigationPlacementPolicy.Normalize(
            SelectedCategoryNavigationPlacement);
        _settings.MaxResults = SearchResultLimitPolicy.Normalize(SelectedMaxResults);
        _settings.SearchAllTileCategories = SearchAllTileCategories;
        _settings.Hotkey = searchHotkey;
        _settings.TileLauncherHotkey = tileLauncherHotkey;
        await _settingsStore.SaveAsync(_settings, cancellationToken);
        _applySearchMode(SelectedSearchMode);
        _applyCategoryNavigationPlacement(_settings.CategoryNavigationPlacement);
        _applyMaxResults(_settings.MaxResults);
        _applySearchAllTileCategories(_settings.SearchAllTileCategories);
        StatusText = "设置已保存";
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
