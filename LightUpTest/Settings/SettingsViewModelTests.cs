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
            applyCategoryNavigationPlacement: placement => appliedPlacement = placement);
        viewModel.SelectedCategoryNavigationPlacement = CategoryNavigationPlacement.Top;

        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CategoryNavigationPlacement.Top, store.Settings.CategoryNavigationPlacement);
        Assert.Equal(CategoryNavigationPlacement.Top, appliedPlacement);
    }

    [Fact]
    public async Task SaveAsync_persists_tile_density_and_notifies_the_tile_host()
    {
        var store = new FakeSettingsStore(new SearchLauncherSettings());
        TileDensity? appliedDensity = null;
        var viewModel = new SettingsViewModel(
            store,
            store.Settings,
            _ => { },
            applyTileDensity: density => appliedDensity = density);
        viewModel.SelectedTileDensity = TileDensity.Comfortable;

        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TileDensity.Comfortable, store.Settings.Appearance.TileDensity);
        Assert.Equal(TileDensity.Comfortable, appliedDensity);
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

    [Fact]
    public async Task SaveAsync_normalizes_hotkeys_and_notifies_the_hotkey_host()
    {
        var store = new FakeSettingsStore(new SearchLauncherSettings());
        (string Search, string Tile)? appliedHotkeys = null;
        var viewModel = new SettingsViewModel(
            store,
            store.Settings,
            _ => { },
            applyHotkeys: (search, tile) =>
            {
                appliedHotkeys = (search, tile);
                return null;
            })
        {
            SearchHotkey = "Ctrl + Alt + K",
            TileLauncherHotkey = "Win + F12"
        };

        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        Assert.Equal("ctrl+alt+k", store.Settings.Hotkey);
        Assert.Equal("win+f12", store.Settings.TileLauncherHotkey);
        Assert.Equal(("ctrl+alt+k", "win+f12"), appliedHotkeys);
    }

    [Fact]
    public async Task SaveAsync_does_not_persist_hotkeys_when_the_hotkey_host_rejects_them()
    {
        var store = new FakeSettingsStore(new SearchLauncherSettings());
        var viewModel = new SettingsViewModel(
            store,
            store.Settings,
            _ => { },
            applyHotkeys: (_, _) => "快捷键被其他程序占用。")
        {
            SearchHotkey = "ctrl+alt+k"
        };

        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        Assert.Equal("alt+space", store.Settings.Hotkey);
        Assert.Equal("快捷键被其他程序占用。", viewModel.StatusText);
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
