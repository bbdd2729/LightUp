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

    public SettingsViewModel(
        ISearchLauncherSettingsStore settingsStore,
        SearchLauncherSettings settings,
        Action<SearchLauncherMode> applySearchMode,
        Action<CategoryNavigationPlacement>? applyCategoryNavigationPlacement = null,
        Action<int>? applyMaxResults = null,
        Action<bool>? applySearchAllTileCategories = null)
    {
        _settingsStore = settingsStore;
        _settings = settings;
        _applySearchMode = applySearchMode;
        _applyCategoryNavigationPlacement = applyCategoryNavigationPlacement ?? (_ => { });
        _applyMaxResults = applyMaxResults ?? (_ => { });
        _applySearchAllTileCategories = applySearchAllTileCategories ?? (_ => { });
        _selectedSearchMode = settings.Mode;
        _selectedMaxResults = SearchResultLimitPolicy.Normalize(settings.MaxResults);
        _searchAllTileCategories = settings.SearchAllTileCategories;
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
    private string _statusText = string.Empty;

    [RelayCommand]
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        _settings.Mode = SelectedSearchMode;
        _settings.CategoryNavigationPlacement = CategoryNavigationPlacementPolicy.Normalize(
            SelectedCategoryNavigationPlacement);
        _settings.MaxResults = SearchResultLimitPolicy.Normalize(SelectedMaxResults);
        _settings.SearchAllTileCategories = SearchAllTileCategories;
        await _settingsStore.SaveAsync(_settings, cancellationToken);
        _applySearchMode(SelectedSearchMode);
        _applyCategoryNavigationPlacement(_settings.CategoryNavigationPlacement);
        _applyMaxResults(_settings.MaxResults);
        _applySearchAllTileCategories(_settings.SearchAllTileCategories);
        StatusText = "设置已保存";
    }
}
