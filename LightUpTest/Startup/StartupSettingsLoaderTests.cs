using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Startup;

public sealed class StartupSettingsLoaderTests
{
    [Fact]
    public async Task LoadAsync_returns_normalized_settings_after_an_async_store_read()
    {
        var settings = await StartupSettingsLoader.LoadAsync(
            new DelayedSettingsStore(new SearchLauncherSettings
            {
                MaxResults = 500,
                Appearance = null!
            }),
            TestContext.Current.CancellationToken);

        Assert.Equal(SearchResultLimitPolicy.MaximumLimit, settings.MaxResults);
        Assert.NotNull(settings.Appearance);
        Assert.NotNull(settings.Appearance.SearchWindow);
    }

    [Fact]
    public async Task LoadAsync_returns_safe_defaults_when_the_store_fails()
    {
        var settings = await StartupSettingsLoader.LoadAsync(
            new ThrowingSettingsStore(),
            TestContext.Current.CancellationToken);

        Assert.Equal(SearchLauncherMode.Full, settings.Mode);
        Assert.Equal("alt+space", settings.Hotkey);
        Assert.Equal("alt+shift+space", settings.TileLauncherHotkey);
        Assert.NotNull(settings.Appearance);
    }

    private sealed class DelayedSettingsStore(SearchLauncherSettings settings) : ISearchLauncherSettingsStore
    {
        public async Task<SearchLauncherSettings> LoadAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            return settings;
        }

        public Task SaveAsync(SearchLauncherSettings settings, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class ThrowingSettingsStore : ISearchLauncherSettingsStore
    {
        public Task<SearchLauncherSettings> LoadAsync(CancellationToken cancellationToken)
            => Task.FromException<SearchLauncherSettings>(new IOException("settings unavailable"));

        public Task SaveAsync(SearchLauncherSettings settings, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
