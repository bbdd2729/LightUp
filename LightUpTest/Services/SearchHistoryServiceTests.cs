using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Services;

public sealed class SearchHistoryServiceTests
{
    [Fact]
    public async Task RecordAsync_puts_the_newest_query_first_and_deduplicates_case_insensitively()
    {
        var settings = new SearchLauncherSettings
        {
            QueryHistory = ["old", "LightUp"]
        };
        var store = new FakeSettingsStore(settings);
        var service = new SearchHistoryService(store, settings);

        await service.RecordAsync("  docs  ", TestContext.Current.CancellationToken);
        await service.RecordAsync("lightup", TestContext.Current.CancellationToken);

        Assert.Equal(["lightup", "docs", "old"], settings.QueryHistory);
        Assert.Equal(2, store.SaveCount);
    }

    [Fact]
    public async Task RecordAsync_ignores_empty_queries_and_respects_the_privacy_switch()
    {
        var settings = new SearchLauncherSettings
        {
            SaveQueryHistory = false
        };
        var store = new FakeSettingsStore(settings);
        var service = new SearchHistoryService(store, settings);

        await service.RecordAsync("", TestContext.Current.CancellationToken);
        await service.RecordAsync("private query", TestContext.Current.CancellationToken);

        Assert.Empty(settings.QueryHistory);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task ClearAsync_removes_all_entries_and_persists_the_change()
    {
        var settings = new SearchLauncherSettings
        {
            QueryHistory = ["one", "two"]
        };
        var store = new FakeSettingsStore(settings);
        var service = new SearchHistoryService(store, settings);

        await service.ClearAsync(TestContext.Current.CancellationToken);

        Assert.Empty(settings.QueryHistory);
        Assert.Equal(1, store.SaveCount);
    }

    private sealed class FakeSettingsStore(SearchLauncherSettings settings) : ISearchLauncherSettingsStore
    {
        public int SaveCount { get; private set; }

        public Task<SearchLauncherSettings> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(settings);

        public Task SaveAsync(SearchLauncherSettings value, CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
