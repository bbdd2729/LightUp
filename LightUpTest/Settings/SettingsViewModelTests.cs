using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpTest.Settings;

public sealed class SettingsViewModelTests
{
    [Fact]
    public async Task SaveAsync_persists_the_selected_search_mode_and_notifies_the_host()
    {
        var store = new FakeSettingsStore(new SearchLauncherSettings());
        SearchLauncherMode? appliedMode = null;
        var viewModel = new SettingsViewModel(store, store.Settings, mode => appliedMode = mode);
        viewModel.SelectedSearchMode = SearchLauncherMode.Simple;

        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        Assert.Equal(SearchLauncherMode.Simple, store.Settings.Mode);
        Assert.Equal(SearchLauncherMode.Simple, appliedMode);
        Assert.Equal("设置已保存", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveAsync_persists_category_navigation_placement_and_notifies_the_tile_host()
    {
        var store = new FakeSettingsStore(new SearchLauncherSettings());
        CategoryNavigationPlacement? appliedPlacement = null;
        var viewModel = new SettingsViewModel(
            store,
            store.Settings,
            _ => { },
            placement => appliedPlacement = placement);
        viewModel.SelectedCategoryNavigationPlacement = CategoryNavigationPlacement.Top;

        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CategoryNavigationPlacement.Top, store.Settings.CategoryNavigationPlacement);
        Assert.Equal(CategoryNavigationPlacement.Top, appliedPlacement);
    }

    [Fact]
    public async Task SaveAsync_normalizes_the_result_limit_and_notifies_the_search_host()
    {
        var store = new FakeSettingsStore(new SearchLauncherSettings());
        int? appliedLimit = null;
        var viewModel = new SettingsViewModel(
            store,
            store.Settings,
            _ => { },
            applyMaxResults: limit => appliedLimit = limit);
        viewModel.SelectedMaxResults = 500;

        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        Assert.Equal(100, store.Settings.MaxResults);
        Assert.Equal(100, appliedLimit);
    }

    [Fact]
    public async Task SaveAsync_persists_tile_category_scope_and_notifies_the_tile_search_provider()
    {
        var store = new FakeSettingsStore(new SearchLauncherSettings());
        bool? appliedSearchAllCategories = null;
        var viewModel = new SettingsViewModel(
            store,
            store.Settings,
            _ => { },
            applySearchAllTileCategories: value => appliedSearchAllCategories = value);
        viewModel.SearchAllTileCategories = false;

        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        Assert.False(store.Settings.SearchAllTileCategories);
        Assert.False(appliedSearchAllCategories);
    }

    private sealed class FakeSettingsStore(SearchLauncherSettings settings) : ISearchLauncherSettingsStore
    {
        public SearchLauncherSettings Settings { get; private set; } = settings;

        public Task<SearchLauncherSettings> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(Settings);

        public Task SaveAsync(SearchLauncherSettings settings, CancellationToken cancellationToken)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
    }
}
