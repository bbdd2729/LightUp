using LightUpUI.Models;
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
