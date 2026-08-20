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

    public SettingsViewModel(
        ISearchLauncherSettingsStore settingsStore,
        SearchLauncherSettings settings,
        Action<SearchLauncherMode> applySearchMode,
        Action<CategoryNavigationPlacement>? applyCategoryNavigationPlacement = null)
    {
        _settingsStore = settingsStore;
        _settings = settings;
        _applySearchMode = applySearchMode;
        _applyCategoryNavigationPlacement = applyCategoryNavigationPlacement ?? (_ => { });
        _selectedSearchMode = settings.Mode;
        _selectedCategoryNavigationPlacement = CategoryNavigationPlacementPolicy.Normalize(
            settings.CategoryNavigationPlacement);
    }

    public Array SearchModes { get; } = Enum.GetValues<SearchLauncherMode>();
    public Array CategoryNavigationPlacements { get; } = Enum.GetValues<CategoryNavigationPlacement>();

    [ObservableProperty]
    private SearchLauncherMode _selectedSearchMode;

    [ObservableProperty]
    private CategoryNavigationPlacement _selectedCategoryNavigationPlacement;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [RelayCommand]
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        _settings.Mode = SelectedSearchMode;
        _settings.CategoryNavigationPlacement = CategoryNavigationPlacementPolicy.Normalize(
            SelectedCategoryNavigationPlacement);
        await _settingsStore.SaveAsync(_settings, cancellationToken);
        _applySearchMode(SelectedSearchMode);
        _applyCategoryNavigationPlacement(_settings.CategoryNavigationPlacement);
        StatusText = "设置已保存";
    }
}
