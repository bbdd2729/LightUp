using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class SearchHistorySearchProviderTests
{
    [Fact]
    public async Task Empty_query_returns_recent_queries_in_recency_order()
    {
        var provider = new SearchHistorySearchProvider(new FakeHistory(["docs", "calculator"]));

        var results = await provider.SearchAsync("", TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Count);
        Assert.Equal("action:search-query:docs", results[0].Id);
        Assert.Equal("docs", results[0].Arguments);
        Assert.Equal(LauncherItemKind.Action, results[0].Kind);
        Assert.Equal("action:search-query:calculator", results[1].Id);
    }

    [Fact]
    public async Task Non_empty_query_does_not_add_history_suggestions_to_normal_search()
    {
        var history = new FakeHistory(["docs"]);
        var provider = new SearchHistorySearchProvider(history);

        var results = await provider.SearchAsync("doc", TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    private sealed class FakeHistory(IReadOnlyList<string> queries) : ISearchHistoryService
    {
        public IReadOnlyList<string> RecentQueries => queries;

        public Task RecordAsync(string query, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
