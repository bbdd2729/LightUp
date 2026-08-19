using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class TileUsageTests
{
    [Fact]
    public async Task RecordLaunchAsync_updates_count_and_timestamp_for_the_saved_tile()
    {
        var tile = new TileItem { Id = "notes", Title = "Notes", TargetPath = "notes.exe" };
        var state = new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [tile] }]
        };
        var store = new FakeStateStore(state);
        var tracker = new TileUsageService(store, () => new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc));

        await tracker.RecordLaunchAsync("notes", TestContext.Current.CancellationToken);

        Assert.Equal(1, tile.LaunchCount);
        Assert.Equal(new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc), tile.LastLaunchedAtUtc);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Tracking_launcher_records_usage_only_after_a_successful_launch()
    {
        var tracker = new FakeUsageTracker();
        var inner = new FakeProcessLauncher(LaunchResult.Success);
        var launcher = new UsageTrackingProcessLauncher(inner, tracker);

        var result = await launcher.LaunchAsync(
            new LauncherItem("notes", "Notes", "", "notes.exe", null, LauncherItemKind.Shortcut),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("notes", tracker.LastItemId);
    }

    private sealed class FakeStateStore(TileLauncherState state) : ILauncherStateStore
    {
        public int SaveCount { get; private set; }

        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(state);

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUsageTracker : ILauncherUsageTracker
    {
        public string? LastItemId { get; private set; }

        public Task RecordLaunchAsync(string itemId, CancellationToken cancellationToken)
        {
            LastItemId = itemId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProcessLauncher(LaunchResult result) : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(LauncherItem item, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }
}
