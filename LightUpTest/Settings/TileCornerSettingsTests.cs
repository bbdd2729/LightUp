using LightUpUI.Models;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpTest.Settings;

public sealed class TileCornerSettingsTests
{
    [Theory]
    [InlineData(0, 700)]
    [InlineData(-1, 700)]
    [InlineData(100, 200)]
    [InlineData(9000, 5000)]
    public void Normalize_clamps_corner_trigger_delay(int value, int expected)
        => Assert.Equal(expected, TileCornerTriggerSettingsPolicy.NormalizeDelay(value));

    [Fact]
    public async Task SaveAsync_persists_corner_trigger_options_and_applies_them()
    {
        var store = new FakeSettingsStore(new SearchLauncherSettings());
        SearchLauncherSettings? applied = null;
        var viewModel = new SettingsViewModel(
            store,
            store.Settings,
            _ => { },
            applyTileCornerSettings: settings => applied = settings)
        {
            EnableTileCornerTrigger = true,
            TileCornerTriggerDelayMilliseconds = 1500,
            CloseTileLauncherOnPointerLeave = false,
            CloseTileLauncherAfterLaunch = false
        };

        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        Assert.True(store.Settings.EnableTileCornerTrigger);
        Assert.Equal(1500, store.Settings.TileCornerTriggerDelayMilliseconds);
        Assert.False(store.Settings.CloseTileLauncherOnPointerLeave);
        Assert.False(store.Settings.CloseTileLauncherAfterLaunch);
        Assert.Same(store.Settings, applied);
    }

    private sealed class FakeSettingsStore(SearchLauncherSettings settings) : ISearchLauncherSettingsStore
    {
        public SearchLauncherSettings Settings { get; private set; } = settings;
        public Task<SearchLauncherSettings> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(Settings);
        public Task SaveAsync(SearchLauncherSettings settings, CancellationToken cancellationToken)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
    }
}
