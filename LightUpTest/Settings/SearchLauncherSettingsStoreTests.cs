using System.Text;
using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Services;

namespace LightUpTest.Settings;

public sealed class SearchLauncherSettingsStoreTests
{
    [Fact]
    public async Task LoadAsync_completes_without_the_caller_processing_its_synchronization_context()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(filePath, "{}", cancellationToken);

        using var stream = new GateReadStream("""
            {"mode":0,"hotkey":"alt+space","maxResults":30,"searchAllTileCategories":true,"plugins":{}}
            """);
        using var releaseWorker = new ManualResetEventSlim(false);
        var started = new TaskCompletionSource<Task<SearchLauncherSettings>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new SearchLauncherSettingsStore(filePath, () => stream);
        var worker = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
            started.SetResult(store.LoadAsync(cancellationToken));
            releaseWorker.Wait();
            SynchronizationContext.SetSynchronizationContext(null);
        })
        {
            IsBackground = true
        };

        try
        {
            worker.Start();
            var loadTask = await started.Task.WaitAsync(
                TimeSpan.FromSeconds(2),
                cancellationToken);
            stream.Release();

            var settings = await loadTask.WaitAsync(
                TimeSpan.FromSeconds(2),
                cancellationToken);

            Assert.Equal(30, settings.MaxResults);
        }
        finally
        {
            releaseWorker.Set();
            worker.Join(TimeSpan.FromSeconds(2));
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task SaveAsync_round_trips_the_category_navigation_placement()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var store = new SearchLauncherSettingsStore(filePath);

        try
        {
            await store.SaveAsync(
                new SearchLauncherSettings
                {
                    CategoryNavigationPlacement = CategoryNavigationPlacement.Top
                },
                cancellationToken);

            var loaded = await store.LoadAsync(cancellationToken);

            Assert.Equal(CategoryNavigationPlacement.Top, loaded.CategoryNavigationPlacement);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task LoadAsync_round_trips_window_appearance_and_normalizes_unknown_enums()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var store = new SearchLauncherSettingsStore(filePath);

        try
        {
            await File.WriteAllTextAsync(filePath, """
                {
                  "mode": 99,
                  "categoryNavigationPlacement": 99,
                  "appearance": {
                    "searchWindow": { "x": 120, "y": 80, "width": 720, "height": 480, "isTopmost": true }
                  }
                }
                """, cancellationToken);

            var loaded = await store.LoadAsync(cancellationToken);

            Assert.Equal(SearchLauncherMode.Full, loaded.Mode);
            Assert.Equal(CategoryNavigationPlacement.Left, loaded.CategoryNavigationPlacement);
            Assert.Equal(120, loaded.Appearance.SearchWindow.X);
            Assert.Equal(720, loaded.Appearance.SearchWindow.Width);
            Assert.True(loaded.Appearance.SearchWindow.IsTopmost);
            Assert.NotNull(loaded.Appearance.TileLauncherWindow);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task LoadAsync_normalizes_theme_and_palette_settings()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var store = new SearchLauncherSettingsStore(filePath);

        try
        {
            await File.WriteAllTextAsync(filePath, """
                {
                  "appearance": {
                    "themeMode": 99,
                    "colorPalette": 99,
                    "customAccentColor": "invalid"
                  }
                }
                """, cancellationToken);

            var loaded = await store.LoadAsync(cancellationToken);

            Assert.Equal(LauncherThemeMode.Dark, loaded.Appearance.ThemeMode);
            Assert.Equal(LauncherColorPalette.Ocean, loaded.Appearance.ColorPalette);
            Assert.Equal(ThemePalettePolicy.DefaultCustomAccentColor, loaded.Appearance.CustomAccentColor);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private sealed class NonPumpingSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state)
        {
        }
    }

    private sealed class GateReadStream : MemoryStream
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public GateReadStream(string content)
            : base(Encoding.UTF8.GetBytes(content))
        {
        }

        public void Release() => _gate.TrySetResult();

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            await _gate.Task.ConfigureAwait(false);
            return await base.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            await _gate.Task.ConfigureAwait(false);
            return await base.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }
    }
}
